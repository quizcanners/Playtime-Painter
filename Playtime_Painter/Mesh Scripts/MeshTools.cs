using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
namespace Playtime_Painter
{

    public enum MeshTool { vertices, uv, VertexAnimation, VertColor, VertexShadow, AtlasTexture, VertexEdge }

    public static class MeshTools
    {

        public static MeshToolBase Get(this MeshTool t)  {
            return allTools[(int)t];
        }

        static MeshTools() {
            List<Type> allTypes = CsharpFuncs.GetAllChildTypesOf<MeshToolBase>();

            allTools = new MeshToolBase[allTypes.Count];

            foreach (Type t in allTypes)
            {
                MeshToolBase tb = (MeshToolBase)Activator.CreateInstance(t);
                allTools[(int)(tb.myTool)] = tb;
            }
        }

        static MeshToolBase[] allTools;


    }

    public abstract class MeshToolBase {

        protected MeshManager m { get { return MeshManager.inst; } }
        protected EditableMesh mesh { get { return MeshManager.inst._Mesh; } }
        protected PainterConfig cfg { get { return PainterConfig.inst; } }
        protected LineData line { get { return m.pointedLine; } }
        protected trisDta triangle { get { return m.pointedTris; } }
        protected UVpoint vertex { get { return m.pointedUV; } }

        public abstract MeshTool myTool { get; }

        public virtual bool showVertices { get { return true; } }
        public virtual bool showLines { get { return true; } }
        public virtual bool showTriangles { get { return true; } }

        public virtual bool showSelectedVerticle { get { return false; } }
        public virtual bool showSelectedLine { get { return false; } }
        public virtual bool showSelectedTriangle { get { return false; } }

        public virtual string tooltip { get { return "No tooltip"; } }

        public virtual void tool_pegi() {
        }

        public virtual void MouseEventPointedVertex() { }

        public virtual void MouseEventPointedLine() { }

        public virtual void MouseEventPointedTriangle() { }

        public virtual void MouseEventPointedNothing() {  }

        public virtual void KeysEventPointedVertex()  { }

        public virtual void KeysEventPointedLine()  { }

        public virtual void KeysEventPointedTriangle()  { }
    }

    public class VertexPositionTool : MeshToolBase
    {
        public static VertexPositionTool inst;

        public VertexPositionTool() {
            inst = this;
        }

        Vector3 displace = new Vector3();
        

        public override MeshTool myTool { get { return MeshTool.vertices; } }

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

        public override void tool_pegi() {

            MeshManager mgm = m;

            PainterConfig sd = PainterConfig.inst;

            if (m.showGrid)
            {
                "Snap to grid:".toggle(100, ref sd.SnapToGrid);

                if (sd.SnapToGrid)
                    "size:".edit(40, ref sd.SnapToGridSize);
            }

            pegi.ClickToEditScript();

            pegi.newLine();

            if ("Auto Bevel".Click())
                AutoAssignDominantNormalsForBeveling();
            "Sensitivity".edit(60,ref cfg.bevelDetectionSensetivity, 3, 30).nl();

            "Pixel-Perfect".toggle("New vertex will have UV coordinate rounded to half a pixel.",120, ref cfg.pixelPerfectMeshEditing).nl();

            "Add Smooth:".toggle(70, ref MeshManager.cfg.newVerticesSmooth);
            if (pegi.Click("Sharp All")) {
                foreach (vertexpointDta vr in m._Mesh.vertices)
                    vr.SmoothNormal = false;
                mgm._Mesh.Dirty = true;
                MeshManager.cfg.newVerticesSmooth = false;
            }

            if (pegi.Click("Smooth All").nl()) {
                foreach (vertexpointDta vr in m._Mesh.vertices)
                    vr.SmoothNormal = true;
                mgm._Mesh.Dirty = true;
                MeshManager.cfg.newVerticesSmooth = true;
            }

            "Add Unique:".toggle(70, ref MeshManager.cfg.newVerticesUnique);
            if (pegi.Click("All shared")) {
                mgm._Mesh.AllVerticesShared();
                mgm._Mesh.Dirty = true;
                MeshManager.cfg.newVerticesUnique = false;
            }

            if (pegi.Click("All unique"))
            {
                foreach (trisDta t in mesh.triangles)
                    mgm._Mesh.GiveTriangleUniqueVerticles(t);
                mgm._Mesh.Dirty = true;
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
                mgm._Mesh.Dirty = true;
                displace = Vector3.zero;
            }
            pegi.newLine();
        }

        public override void KeysEventPointedVertex()
        {
  
            if (m.pointedUV == null) return;

            if ((KeyCode.M.isDown()) && (m.draggingSelected))
            {
                m.selectedUV.vert.MergeWithNearest();
                m.draggingSelected = false;
                m._Mesh.Dirty = true;
                "M - merge with nearest".TeachingNotification();
            }

            if (KeyCode.N.isDown()) {
                m.pointedUV.vert.SmoothNormal = !m.pointedUV.vert.SmoothNormal;
                m._Mesh.Dirty = true;
                "N - on Vertex - smooth Normal".TeachingNotification();
            }

            if (KeyCode.Delete.isDown())
            {
                //Debug.Log("Deleting");
                if (!EditorInputManager.getControlKey())
                {
                    if (m.pointedUV.vert.uv.Count == 1)
                    {
                        if (!m.DeleteVertHEAL(m.pointedUV.vert))
                            m.DeleteUv(m.pointedUV);
                    }
                    else
                    {
                        while (m.pointedUV.vert.uv.Count > 1)
                        {
                            m._Mesh.MoveTris(m.pointedUV.vert.uv[1], m.pointedUV.vert.uv[0]); //DeleteUv(pointedAt);
                        }
                        //Debug.Log("Healing");
                    }

                }
                else
                    m.DeleteUv(m.pointedUV);
                m._Mesh.Dirty = true;
            }
        }

        public override void KeysEventPointedLine()
        {
            if (KeyCode.N.isDown()) {
                foreach (var t in m.pointedLine.getAllTriangles_USES_Tris_Listing())
                    t.SetDominantNormals(!EditorInputManager.getAltKey());

                "N ON A LINE - Make triangle normals Dominant".TeachingNotification();

                m._Mesh.Dirty = true;
            }
        }

        public override void KeysEventPointedTriangle()
        {
            if (KeyCode.Backspace.isDown())
            {
                m._Mesh.triangles.Remove(triangle);
                m._Mesh.Dirty = true;
                return;
            }

            if (KeyCode.U.isDown())
                triangle.MakeTriangleVertUnique(vertex);

            if (KeyCode.N.isDown())
            {

                if (!EditorInputManager.getAltKey())
                {
                    int no = triangle.NumberOf(triangle.GetClosestTo(m.collisionPosLocal));
                    triangle.DominantNormals[no] = !triangle.DominantNormals[no];

                    (triangle.DominantNormals[no] ? "Triangle edge's Normal is now dominant" : "Triangle edge Normal is NO longer dominant").TeachingNotification();
                }
                else
                {
                    triangle.InvertNormal();
                    "Inverting Normals".TeachingNotification();
                }

                m._Mesh.Dirty = true;

            }


        }

        public override void MouseEventPointedVertex()
        {
         

            //Debug.Log("Mouse event pointed vertex");

            if (EditorInputManager.GetMouseButtonDown(0))
            {
                if ((m.trisVerts < 3) && (m.pointedUV != null) && (!m.isInTrisSet(m.pointedUV.vert)))
                    m.AddToTrisSet(m.pointedUV);

              
            }

            if ((EditorInputManager.GetMouseButtonDown(1)))
            {
                if ((EditorInputManager.getAltKey() && (m.selectedUV.vert.uv.Count>1)))
                    m.DisconnectDragged();
                m.draggingSelected = true;
                m.dragDelay = 0.2f;
            }

        }

        public override void MouseEventPointedLine() {
            if (EditorInputManager.GetMouseButtonDown(0)) 
                m._Mesh.insertIntoLine(m.pointedLine.pnts[0].vert, m.pointedLine.pnts[1].vert, m.collisionPosLocal);
            
        }

        public override void MouseEventPointedTriangle() {
            if (EditorInputManager.GetMouseButtonDown(0))  {
                if (MeshManager.cfg.newVerticesUnique)
                    m._Mesh.insertIntoTriangleUniqueVerticles(m.pointedTris, m.collisionPosLocal);
                else
                    m._Mesh.insertIntoTriangle(m.pointedTris, m.collisionPosLocal);
            }
        }

        public override void MouseEventPointedNothing() {
            if (EditorInputManager.GetMouseButtonDown(0))
                m.AddPoint(m.onGridLocal);
        }

        public void AutoAssignDominantNormalsForBeveling() {

            foreach (vertexpointDta vr in m._Mesh.vertices)
                vr.SmoothNormal = true;
           
            foreach (var t in mesh.triangles) t.SetDominantNormals(true);

                foreach (var t in mesh.triangles) {
                Vector3[] v3s = new Vector3[3];
                
                for (int i=0; i<3; i++)
                    v3s[i] = t.uvpnts[i].vert.pos;

                float[] dist = new float[3];

                for (int i = 0; i < 3; i++) 
                    dist[i] = (v3s[(i + 1) % 3] - v3s[(i + 2) % 3]).magnitude;

                for (int i = 0; i < 3; i++) {
                    var a = (i + 1) % 3;
                    var b = (i + 2) % 3;
                    if (dist[i] < dist[a] / cfg.bevelDetectionSensetivity && dist[i] < dist[b] / cfg.bevelDetectionSensetivity) {
                        t.SetDominantNormals(false);

                        var other = (new LineData(t, t.uvpnts[a], t.uvpnts[b])).getOtherTriangle();
                        if (other != null)
                            other.SetDominantNormals(false);
                    }
                }
            }

            mesh.Dirty = true;
        }

    }

    public class VertexUVTool : MeshToolBase
    {

        public override MeshTool myTool { get { return MeshTool.uv; } }

        public override void tool_pegi()
        {
            MeshManager tmp = m;

            pegi.write("Edit UV 1:", 70);
            pegi.toggleInt (ref MeshManager.editedUV);
            pegi.newLine();

            if (m.selectedUV != null)
                if (pegi.Click("ALL from selected")) {
                    foreach (vertexpointDta vr in m._Mesh.vertices)
                        foreach (UVpoint uv in vr.uv)
                            uv.editedUV = m.selectedUV.editedUV;

                    m._Mesh.Dirty = true;
                }

            pegi.newLine();

            if (m.selectedUV != null)
                pegi.write("UV: " + (tmp.selectedUV.editedUV.x) + "," + (tmp.selectedUV.editedUV.y));

            pegi.newLine();
            if (m.GridToUVon) {
                pegi.write("Projection size");
                if (pegi.edit(ref MeshManager.cfg.MeshUVprojectionSize))
                    m.ProcessScaleChange();
            }
            pegi.newLine();

           if  (pegi.toggle(ref tmp.GridToUVon, "Grid Painting ", 90)) {
                if (!tmp.GridToUVon)
                    cfg._meshTool = MeshTool.uv;
                tmp.UpdatePreviewIfGridedDraw();
            }

            pegi.newLine();

            if (tmp.selectedUV != null)
                if (pegi.Click("All vert UVs from selected"))  {
                    foreach (UVpoint uv in tmp.selectedUV.vert.uv)
                        uv.editedUV = tmp.selectedUV.editedUV;

                    tmp._Mesh.Dirty = true;
                }


            pegi.newLine();


        }

        public override void MouseEventPointedVertex()
        {
         

            if (EditorInputManager.GetMouseButtonDown(0))
            {
                if ((m.selectedUV != null) && (m.pointedUV != null))
                {
                    m.pointedUV.editedUV = m.selectedUV.editedUV;
                    m._Mesh.Dirty = true;
                }

              
            }

            if ((EditorInputManager.GetMouseButtonDown(1)) && (m.pointedUV != null) && (UVnavigator.inst() != null))
                UVnavigator.inst().CenterOnUV(m.pointedUV.editedUV);

        }

        public override void MouseEventPointedLine()
        {
            UVpoint a = line.pnts[0];
            UVpoint b = line.pnts[1];

            if (Vector3.Distance(m.collisionPosLocal, a.vert.pos) < Vector3.Distance(m.collisionPosLocal, b.vert.pos))
                m.AssignSelected(m._Mesh.GetUVpointAFromLine(a.vert, b.vert));
            else
                m.AssignSelected(m._Mesh.GetUVpointAFromLine(b.vert, a.vert));

        }

        public override void MouseEventPointedTriangle()  {

                if (EditorInputManager.GetMouseButtonDown(0))  {
                        if (m.GridToUVon) {
                            if (m.selectedUV == null) m.selectedUV = m._Mesh.vertices[0].uv[0];

                            for (int i = 0; i < 3; i++)
                                m.pointedTris.uvpnts[i].editedUV = m.PosToUV(m.pointedTris.uvpnts[i].vert.pos);
                            m._Mesh.Dirty = true;
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
                    m.SwapLine(a.vert, b.vert);
                else
                    m.DeleteLine(line);

                m._Mesh.Dirty = true;
            }
        }

    }

    public class VertexAnimationTool : MeshToolBase
    {
        public override MeshTool myTool { get { return MeshTool.VertexAnimation; } }

        public override void MouseEventPointedVertex()
        {
          
            if (m.pointedUV == null) return;
            if (EditorInputManager.GetMouseButtonDown(1))
            {
                m.draggingSelected = true;
                m.dragDelay = 0.2f;
            }
        }
    }

    public class VertexColorTool : MeshToolBase
    {
        public override MeshTool myTool { get { return MeshTool.VertColor; } }

        public override void tool_pegi() {
            if (("Paint All with Brush Color").Click())
                m._Mesh.PaintAll(cfg.brushConfig.color);

            "Make Vertices Unique On coloring".toggle(60, ref cfg.MakeVericesUniqueOnEdgeColoring).nl();

            Color col = cfg.brushConfig.color.ToColor();
            if (pegi.edit(ref col).nl())
                cfg.brushConfig.color.From(col);

            cfg.brushConfig.ColorSliders_PEGI().nl();

            pegi.writeHint("Ctrl+LMB on Vertex - to paint only selected uv");
        }

        public override void MouseEventPointedVertex()  {
            MeshManager mgmt = m;

            BrushConfig bcf = cfg.brushConfig;

            if (EditorInputManager.GetMouseButtonDown(1))
                mgmt.pointedUV.vert.clearColor(cfg.brushConfig.mask);

            if ((EditorInputManager.GetMouseButtonDown(0))) {
                if (EditorInputManager.getControlKey())
                
                    bcf.mask.Transfer(ref mgmt.pointedUV._color, bcf.color.ToColor());
                
                else
                    foreach (UVpoint uvi in mgmt.pointedUV.vert.uv)
                        bcf.mask.Transfer(ref uvi._color, cfg.brushConfig.color.ToColor());

                mgmt._Mesh.Dirty = true;
            }
        }

        public override void MouseEventPointedLine()
        {
            if (EditorInputManager.GetMouseButtonDown(0))  {
                BrushConfig bcf = cfg.brushConfig;

                UVpoint a = line.pnts[0];
                UVpoint b = line.pnts[1];
            
                Color c = bcf.color.ToColor();
               
                a.vert.SetColorOnLine(c, bcf.mask, b.vert);//setColor(glob.colorSampler.color, glob.colorSampler.mask);
                b.vert.SetColorOnLine(c, bcf.mask, a.vert);
                m._Mesh.Dirty = true;
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
                m._Mesh.Dirty = true;
                Event.current.Use();
            }
          
        }

    }

    public class VertexEdgeTool : MeshToolBase
    {
        public override MeshTool myTool { get { return MeshTool.VertexEdge; } }

        public override bool showTriangles { get { return false; } }

        public static float edgeValue = 1;

        public override void tool_pegi() {
            "Edge Strength: ".edit(ref edgeValue).nl();

            pegi.writeHint("Strength usually used to show color on the edge and hide seam");
        }

        public override void MouseEventPointedVertex()
        {
          

            if ((EditorInputManager.GetMouseButtonDown(0)))
            {
                if (EditorInputManager.getControlKey())
                    edgeValue = m.pointedUV.vert.edgeStrength;
                else
                    m.pointedUV.vert.edgeStrength = edgeValue;


                m._Mesh.Dirty = true;
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


                    m._Mesh.Dirty = true;
                }
            }
        }
        
    }

    public class VertexShadowTool : MeshToolBase
    {
        public override MeshTool myTool { get { return MeshTool.VertexShadow; } }

        public override void MouseEventPointedVertex()
        {
          

            if (m.pointedUV == null) return;

            if ((EditorInputManager.GetMouseButtonDown(0)))
            {
                BrushConfig bcf = cfg.brushConfig;
                bcf.color.ToV4(ref m.pointedUV.vert.shadowBake, bcf.mask);
                Debug.Log("Modified shadow to: " + m.pointedUV.vert.shadowBake.ToString());

                m._Mesh.Dirty = true;
            }
        }

    }

    public class VertexAtlasTool : MeshToolBase
    {
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

        static VertexAtlasTool _inst;

		public static VertexAtlasTool inst {get { if (_inst == null)
					MeshTool.AtlasTexture.Get ();
				return _inst;}}

        public VertexAtlasTool()
        {
            _inst = this;
        }

        public override MeshTool myTool { get { return MeshTool.AtlasTexture; } }

        public override void tool_pegi() {

            "Edge Click as Chanel 2".toggle(ref cfg.atlasEdgeAsChanel2).nl();

            "Atlas Texture: ".edit(ref cfg.curAtlasTexture).nl();
            "Atlas Chanel: ".edit(ref cfg.curAtlasChanel).nl();

            if (m.selectedTris != null)
            {
                ("Selected tris uses Atlas Texture " + m.selectedTris.textureNo[0]).nl();
            }

            pegi.writeHint("Cntrl + LMB -> Sample Texture");
        }

        public override void MouseEventPointedTriangle()
        {
           
            if (EditorInputManager.GetMouseButtonDown(0))
            {
                if (EditorInputManager.getControlKey())
                    cfg.curAtlasTexture = (int)m.pointedTris.textureNo[cfg.curAtlasChanel];
                else
                {
                    m.pointedTris.textureNo[cfg.curAtlasChanel] = cfg.curAtlasTexture;
                    m._Mesh.Dirty = true;
                }
            }
        }

        public override void MouseEventPointedLine() {
            if (EditorInputManager.GetMouseButtonDown(0))
            {

               foreach(var t in m.pointedLine.getAllTriangles_USES_Tris_Listing()) 
                    t.textureNo.y = cfg.curAtlasTexture;
                m._Mesh.Dirty = true;

            }

        }

        public void SetAllTrianglesTextureTo(int no, int chanel) {

            foreach (trisDta t in mesh.triangles)
                t.textureNo[chanel] = no;

            m._Mesh.Dirty = true;
        }

        public override void KeysEventPointedTriangle()
        {


            int keyDown = Event.current.NumericKeyDown();
            
            if (keyDown!= -1) {
                m.pointedTris.textureNo[cfg.curAtlasChanel] = keyDown;
                m._Mesh.Dirty = true;
                if (!Application.isPlaying) Event.current.Use();
            }
        
    }

    }

}