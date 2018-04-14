using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
namespace Playtime_Painter
{

    public abstract class MeshToolBase : PainterStuff {
        
        static List<MeshToolBase> _allTools;

        public static List<MeshToolBase> allTools
        { get {
                if (_allTools == null) {
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
        protected LineData line { get { return meshMGMT.pointedLine; } }
        protected trisDta triangle { get { return meshMGMT.pointedTris; } }
        protected UVpoint uvPoint { get { return meshMGMT.pointedUV; } }
        protected vertexpointDta vertex { get { return meshMGMT.pointedUV.vert; } }

        public virtual bool showVertices { get { return true; } }
        public virtual bool showLines { get { return true; } }
        public virtual bool showTriangles { get { return true; } }

        public virtual bool showGrid { get { return false; } }

        public virtual bool showSelectedVerticle { get { return false; } }
        public virtual bool showSelectedLine { get { return false; } }
        public virtual bool showSelectedTriangle { get { return false; } }

        public virtual string tooltip { get { return "No tooltip"; } }

        public virtual Color vertColor {get { return Color.gray;  }  }

        public virtual bool PEGI() {
            return false;
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

            var pvrt = meshMGMT.GetSelectedVert();

            if ((vpoint.uv.Count > 1) || (pvrt == vpoint))
            {

                Texture tex = meshMGMT.target.meshRenderer.sharedMaterial.mainTexture;

                if (pvrt == vpoint)
                {
                    mrkr.textm.text = (vpoint.uv.Count > 1) ? ((vpoint.uv.IndexOf(meshMGMT.selectedUV) + 1).ToString() + "/" + vpoint.uv.Count.ToString() +
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

        public override bool PEGI() {

            bool changed = false;

            MeshManager mgm = meshMGMT;

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

            "Add Smooth:".toggle(70, ref cfg.newVerticesSmooth);
            if (pegi.Click("Sharp All")) {
                foreach (vertexpointDta vr in meshMGMT.edMesh.vertices)
                    vr.SmoothNormal = false;
                mgm.edMesh.dirty = true;
                cfg.newVerticesSmooth = false;
            }

            if (pegi.Click("Smooth All").nl()) {
                foreach (vertexpointDta vr in meshMGMT.edMesh.vertices)
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
                foreach (trisDta t in mesh.triangles)
                    mgm.edMesh.GiveTriangleUniqueVerticles(t);
                mgm.edMesh.dirty = true;
                cfg.newVerticesUnique = true;
            }
            pegi.newLine();

         
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

            return changed;
        }

        public override void KeysEventPointedVertex()
        {

            var m = meshMGMT;

            if (m.pointedUV == null) return;

            if ((KeyCode.M.isDown()) && (m.draggingSelected))
            {
                m.selectedUV.vert.MergeWithNearest();
                m.draggingSelected = false;
                m.edMesh.dirty = true;
                "M - merge with nearest".TeachingNotification();
            }

            if (KeyCode.N.isDown()) {
                m.pointedUV.vert.SmoothNormal = !m.pointedUV.vert.SmoothNormal;
                m.edMesh.dirty = true;
                "N - on Vertex - smooth Normal".TeachingNotification();
            }

            if (KeyCode.Delete.isDown())
            {
                //Debug.Log("Deleting");
                if (!EditorInputManager.getControlKey())
                {
                    if (m.pointedUV.vert.uv.Count == 1)
                    {
                        if (!m.DeleteVertHEAL(meshMGMT.pointedUV.vert))
                            m.DeleteUv(meshMGMT.pointedUV);
                    }
                    else
                    {
                        while (m.pointedUV.vert.uv.Count > 1)
                        {
                            m.edMesh.MoveTris(m.pointedUV.vert.uv[1], m.pointedUV.vert.uv[0]); //DeleteUv(pointedAt);
                        }
                        //Debug.Log("Healing");
                    }

                }
                else
                    m.DeleteUv(m.pointedUV);
                m.edMesh.dirty = true;
            }
        }

        public override void KeysEventPointedLine()
        {
            if (KeyCode.N.isDown()) {
                foreach (var t in meshMGMT.pointedLine.getAllTriangles_USES_Tris_Listing())
                    t.SetSharpCorners(!EditorInputManager.getAltKey());

                "N ON A LINE - Make triangle normals Dominant".TeachingNotification();

                meshMGMT.edMesh.dirty = true;
            }
        }

        public override void KeysEventPointedTriangle()
        {
            if (KeyCode.Backspace.isDown())
            {
                meshMGMT.edMesh.triangles.Remove(triangle);
                meshMGMT.edMesh.dirty = true;
                return;
            }

            if (KeyCode.U.isDown())
                triangle.MakeTriangleVertUnique(uvPoint);

            if (KeyCode.N.isDown())
            {

                if (!EditorInputManager.getAltKey())
                {
                    int no = triangle.NumberOf(triangle.GetClosestTo(meshMGMT.collisionPosLocal));
                    triangle.SharpCorner[no] = !triangle.SharpCorner[no];

                    (triangle.SharpCorner[no] ? "Triangle edge's Normal is now dominant" : "Triangle edge Normal is NO longer dominant").TeachingNotification();
                }
                else
                {
                    triangle.InvertNormal();
                    "Inverting Normals".TeachingNotification();
                }

                meshMGMT.edMesh.dirty = true;

            }


        }

        public override void MouseEventPointedVertex()
        {
            var m = meshMGMT;

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
                meshMGMT.edMesh.insertIntoLine(meshMGMT.pointedLine.pnts[0].vert, meshMGMT.pointedLine.pnts[1].vert, meshMGMT.collisionPosLocal);
            
        }

        public override void MouseEventPointedTriangle() {
            if (EditorInputManager.GetMouseButtonDown(0))  {
                if (cfg.newVerticesUnique)
                    meshMGMT.edMesh.insertIntoTriangleUniqueVerticles(meshMGMT.pointedTris, meshMGMT.collisionPosLocal);
                else
                    meshMGMT.edMesh.insertIntoTriangle(meshMGMT.pointedTris, meshMGMT.collisionPosLocal);
            }
        }

        public override void MouseEventPointedNothing() {
            if (EditorInputManager.GetMouseButtonDown(0))
                meshMGMT.AddPoint(meshMGMT.onGridLocal);
        }

        public override void ManageDragging()
        {
            var m = meshMGMT;

            if ((EditorInputManager.GetMouseButtonUp(1)) || (EditorInputManager.GetMouseButton(1) == false))
            {
                m.draggingSelected = false;
                mesh.dirty = true;

            }
            else
            {
                m.dragDelay -= Time.deltaTime;
                if ((m.dragDelay < 0) || (Application.isPlaying == false))
                {
                    if (m.selectedUV == null) { m.draggingSelected = false; Debug.Log("no selected"); return; }
                    if ((GridNavigator.inst().angGridToCamera(GridNavigator.onGridPos) < 82))
                                m.selectedUV.vert.pos = m.onGridLocal;
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
                foreach (vertexpointDta vr in meshMGMT.edMesh.vertices)
                    vr.SmoothNormal = false;
                m.edMesh.dirty = true;
                cfg.newVerticesSmooth = false;
            }

            if ("Smooth All".Click().nl()) {
                foreach (vertexpointDta vr in meshMGMT.edMesh.vertices)
                    vr.SmoothNormal = true;
                m.edMesh.dirty = true;
                cfg.newVerticesSmooth = true;
            }


            return false;
            
        }

        public override void MouseEventPointedTriangle() {
            if (EditorInputManager.GetMouseButton(0))
                mesh.dirty |= triangle.SetSharpCorners(SetTo);
        }

        public void AutoAssignDominantNormalsForBeveling()
        {

            foreach (vertexpointDta vr in meshMGMT.edMesh.vertices)
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

        public override bool PEGI() {
            if (("Paint All with Brush Color").Click())
                meshMGMT.edMesh.PaintAll(cfg.brushConfig.colorLinear);

            "Make Vertices Unique On coloring".toggle(60, ref cfg.MakeVericesUniqueOnEdgeColoring).nl();
            
            cfg.brushConfig.ColorSliders_PEGI().nl();

            pegi.writeHint("Ctrl+LMB on Vertex - to paint only selected uv");
            return false;
        }

        public override void MouseEventPointedVertex()  {
            MeshManager m = meshMGMT;

            BrushConfig bcf = cfg.brushConfig;

            if (EditorInputManager.GetMouseButtonDown(1))
                m.pointedUV.vert.clearColor(cfg.brushConfig.mask);

            if ((EditorInputManager.GetMouseButtonDown(0))) {
                if (EditorInputManager.getControlKey())
                
                    bcf.mask.Transfer(ref m.pointedUV._color, bcf.colorLinear.ToGamma());
                
                else
                    foreach (UVpoint uvi in m.pointedUV.vert.uv)
                        bcf.mask.Transfer(ref uvi._color, cfg.brushConfig.colorLinear.ToGamma());

                m.edMesh.dirty = true;
            }
        }

        public override void MouseEventPointedLine()
        {
            if (EditorInputManager.GetMouseButtonDown(0))  {
                BrushConfig bcf = cfg.brushConfig;

                UVpoint a = line.pnts[0];
                UVpoint b = line.pnts[1];
            
                Color c = bcf.colorLinear.ToGamma();
               
                a.vert.SetColorOnLine(c, bcf.mask, b.vert);//setColor(glob.colorSampler.color, glob.colorSampler.mask);
                b.vert.SetColorOnLine(c, bcf.mask, a.vert);
                meshMGMT.edMesh.dirty = true;
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
                meshMGMT.edMesh.dirty = true;
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

        public override bool PEGI() {
            "Edge Strength: ".edit(ref edgeValue).nl();

            pegi.writeHint("Strength usually used to show color on the edge and hide seam");

            return false;
        }

        public override void MouseEventPointedVertex()
        {
          

            if ((EditorInputManager.GetMouseButtonDown(0)))
            {
                if (EditorInputManager.getControlKey())
                    edgeValue = meshMGMT.pointedUV.vert.edgeStrength;
                else
                    meshMGMT.pointedUV.vert.edgeStrength = edgeValue;


                meshMGMT.edMesh.dirty = true;
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


                    meshMGMT.edMesh.dirty = true;
                }
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
                cfg.curSubmesh = (int)meshMGMT.pointedTris.submeshIndex;
                ("Submesh " + cfg.curSubmesh).showNotification();
            }

            if (EditorInputManager.GetMouseButton(0) && !EditorInputManager.getControlKey() && (meshMGMT.pointedTris.submeshIndex != cfg.curSubmesh))  {
                    meshMGMT.pointedTris.submeshIndex = cfg.curSubmesh;
                    mesh.submeshCount = Mathf.Max(meshMGMT.pointedTris.submeshIndex + 1, mesh.submeshCount);
                    mesh.dirty = true;



                }
            
        }
        
        public override bool PEGI()
        {
            ("Total Submeshes: " + mesh.submeshCount).nl();

            "Submesh: ".edit(60, ref cfg.curSubmesh).nl();
            return false;
        }
    }

    public class TriangleAtlasTool : MeshToolBase {

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

        public override bool PEGI() {

            "Edge Click as Chanel 2".toggle(ref cfg.atlasEdgeAsChanel2).nl();

            "Atlas Texture: ".edit(ref cfg.curAtlasTexture).nl();
            "Atlas Chanel: ".edit(ref cfg.curAtlasChanel).nl();

            if (meshMGMT.selectedTris != null)
            {
                ("Selected tris uses Atlas Texture " + meshMGMT.selectedTris.textureNo[0]).nl();
            }

            pegi.writeHint("Cntrl + LMB -> Sample Texture");
            return false;
        }

        public override void MouseEventPointedTriangle()
        {
           
            if (EditorInputManager.GetMouseButton(0))
            {
                if (EditorInputManager.getControlKey())
                    cfg.curAtlasTexture = (int)meshMGMT.pointedTris.textureNo[cfg.curAtlasChanel];
                else
                {
                    meshMGMT.pointedTris.textureNo[cfg.curAtlasChanel] = cfg.curAtlasTexture;
                    meshMGMT.edMesh.dirty = true;
                }
            }
        }

        public override void MouseEventPointedLine() {
            if (EditorInputManager.GetMouseButtonDown(0))
            {

               foreach(var t in meshMGMT.pointedLine.getAllTriangles_USES_Tris_Listing()) 
                    t.textureNo.y = cfg.curAtlasTexture;
                meshMGMT.edMesh.dirty = true;

            }

        }

        public void SetAllTrianglesTextureTo(int no, int chanel) {

            foreach (trisDta t in mesh.triangles)
                t.textureNo[chanel] = no;

            meshMGMT.edMesh.dirty = true;
        }

        public void SetAllTrianglesTextureTo(int no, int chanel, int submesh)
        {

            foreach (trisDta t in mesh.triangles)
                if (t.submeshIndex == submesh)
                    t.textureNo[chanel] = no;

            meshMGMT.edMesh.dirty = true;
        }

        public override void KeysEventPointedTriangle()
        {


            int keyDown = Event.current.NumericKeyDown();
            
            if (keyDown!= -1) {
                meshMGMT.pointedTris.textureNo[cfg.curAtlasChanel] = keyDown;
                meshMGMT.edMesh.dirty = true;
                if (!Application.isPlaying) Event.current.Use();
            }
        
    }

    }

}