using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
namespace Painter
{

    public enum MeshTool { vertices, uv, VertexAnimation, VertColor, VertexShadow, AtlasTexture }

    public static class MeshTools
    {

        public static MeshToolBase Process(this MeshTool t)  {
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

        protected MeshManager m { get { return MeshManager.inst(); } }
        protected playtimeMesherSaveData cfg { get { return playtimeMesherSaveData.inst(); } }
        protected LineData line { get { return m.pointedLine; } }
        protected trisDta triangle { get { return m.pointedTris; } }
        protected UVpoint vertex { get { return m.pointedUV; } }

        public abstract MeshTool myTool { get; }

        public virtual bool showVertices { get { return true; } }
        public virtual bool showLines { get { return true; } }
        public virtual bool showTriangles { get { return true; } }

        public virtual string tooltip { get { return "No tooltip"; } }

        public virtual void tool_pegi() {
        }

        public virtual void MouseEventPointedVertex()
        {

        }

        public virtual void MouseEventPointedLine()
        {

        }

        public virtual void MouseEventPointedTriangle()
        {

        }

        public virtual void MouseEventPointedNothing() {

        }

        public virtual void KeysEventPointedVertex()
        {

        }

        public virtual void KeysEventPointedLine()
        {

        }

        public virtual void KeysEventPointedTriangle()
        {

        }
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
                return "U - make triengle unique."+Environment.NewLine+" M - merge with nearest while dragging " + Environment.NewLine + "N - smooth verticle";
            }
        }

        public override void tool_pegi() {

            MeshManager mgm = m;

            if (pegi.Click("ALL Edges Smooth")) {
                foreach (vertexpointDta vr in m._Mesh.vertices)
                    vr.SmoothNormal = true;

                mgm._Mesh.Dirty = true;
            }

            if (pegi.Click("All verticles shared")) {
                mgm._Mesh.SMOOTHALLVERTS();
                mgm._Mesh.Dirty = true;
            }

            if (pegi.Click("All verticles unique"))
            {
                foreach (trisDta t in MeshManager.inst()._Mesh.triangles)
                    mgm._Mesh.GiveTriangleUniqueVerticles(t);
                mgm._Mesh.Dirty = true;
            }
            pegi.newLine();

         
            if (pegi.Click("Mirror Agains Center")) {
                GridNavigator.onGridPos = mgm._target.p.transform.position;
                mgm.UpdateLocalSpaceV3s();
                mgm._Mesh.MirrorVerticlesAgainsThePlane(mgm.onGridLocal);
            }

            if (pegi.Click("Mirror Agains The Plane")) {
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
            MeshManager m = MeshManager.inst();

            if (m.pointedUV == null) return;

            if ((KeyCode.M.KeyDown()) && (m.draggingSelected))
                m.selectedUV.vert.MergeWithNearest();

            if (KeyCode.Delete.KeyDown())
            {
                Debug.Log("Deleting");
                if (!EditorInputManager.getControlKey())
                {
                    if (m.pointedUV.vert.uv.Count == 1)
                        m.DeleteVertHEAL(m.pointedUV.vert);
                    else
                    {
                        while (m.pointedUV.vert.uv.Count > 1)
                        {
                            m._Mesh.MoveTris(m.pointedUV.vert.uv[1], m.pointedUV.vert.uv[0]); //DeleteUv(pointedAt);
                        }
                        Debug.Log("Healing");
                    }

                }
                else
                    m.DeleteUv(m.pointedUV);
                m._Mesh.Dirty = true;
            }
        }

        public override void KeysEventPointedTriangle()
        {
            if (KeyCode.Backspace.KeyDown())
            {
                m._Mesh.triangles.Remove(triangle);
                m._Mesh.Dirty = true;
                return;
            }

            if (KeyCode.U.KeyDown())
                triangle.MakeTriangleVertUnique(vertex);

            if (KeyCode.N.KeyDown())
            {

                if (EditorInputManager.getAltKey() == false)
                {
                    int no = triangle.NumberOf(triangle.GetClosestTo(m.collisionPosLocal));
                    triangle.ForceSmoothedNorm[no] = !triangle.ForceSmoothedNorm[no];
                }
                else
                {
                    Debug.Log("Inverting normal");
                    triangle.InvertNormal();
                }

                m._Mesh.Dirty = true;

            }


        }

        public override void MouseEventPointedVertex()
        {
            MeshManager m = MeshManager.inst();

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

        public override void MouseEventPointedLine()
        {
            if (EditorInputManager.GetMouseButtonDown(0))
            {
                m._Mesh.insertIntoLine(m.pointedLine.pnts[0].vert, m.pointedLine.pnts[1].vert, m.collisionPosLocal);
            }
        }

        public override void MouseEventPointedTriangle()
        {
            if (EditorInputManager.GetMouseButtonDown(0))
            {
                if (EditorInputManager.getAltKey() == MeshManager.cfg.newVerticesSmooth)
                    m._Mesh.insertIntoTriangleUniqueVerticles(m.pointedTris, m.collisionPosLocal);
                else
                    m._Mesh.insertIntoTriangle(m.pointedTris, m.collisionPosLocal);
            }
        }

        public override void MouseEventPointedNothing() {
            if (EditorInputManager.GetMouseButtonDown(0))
                m.AddPoint(m.onGridLocal);
        }

    }

    public class VertexUVTool : MeshToolBase
    {

        public override MeshTool myTool { get { return MeshTool.uv; } }

        public override void tool_pegi()
        {
            MeshManager tmp = m;

            pegi.write("Editing UV:", 70);
            pegi.edit(ref MeshManager.curUV, 0, 2);
            pegi.newLine();

            if (m.selectedUV != null)
                if (pegi.Click("ALL from selected")) {
                    foreach (vertexpointDta vr in m._Mesh.vertices)
                        foreach (UVpoint uv in vr.uv)
                            uv.uv = m.selectedUV.uv;

                    m._Mesh.Dirty = true;
                }

            pegi.newLine();

            if (m.selectedUV != null)
                pegi.write("UV: " + (tmp.selectedUV.uv.x) + "," + (tmp.selectedUV.uv.y));

            pegi.newLine();
            if (MeshManager.inst().GridToUVon) {
                pegi.write("Projection size");
                if (pegi.edit(ref MeshManager.cfg.MeshUVprojectionSize))
                    MeshManager.inst().ProcessScaleChange();
            }
            pegi.newLine();

           if  (pegi.toggle(ref tmp.GridToUVon, "Grid Painting ", 90)) {
                if (!tmp.GridToUVon)
                    playtimeMesherSaveData.inst()._meshTool = MeshTool.uv;
                tmp.UpdatePreviewIfGridedDraw();
            }

            pegi.newLine();

            if (tmp.selectedUV != null)
                if (pegi.Click("All vert UVs from selected"))  {
                    foreach (UVpoint uv in tmp.selectedUV.vert.uv)
                        uv.uv = tmp.selectedUV.uv;

                    tmp._Mesh.Dirty = true;
                }


            pegi.newLine();


        }

        public override void MouseEventPointedVertex()
        {
            MeshManager m = MeshManager.inst();

            if (EditorInputManager.GetMouseButtonDown(0))
            {
                if ((m.selectedUV != null) && (m.pointedUV != null))
                {
                    m.pointedUV.uv = m.selectedUV.uv;
                    m._Mesh.Dirty = true;
                }

              
            }

            if ((EditorInputManager.GetMouseButtonDown(1)) && (m.pointedUV != null) && (UVnavigator.inst() != null))
                UVnavigator.inst().CenterOnUV(m.pointedUV.uv);

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
                                m.pointedTris.uvpnts[i].uv = m.PosToUV(m.pointedTris.uvpnts[i].vert.pos);
                            m._Mesh.Dirty = true;
                        }
            }
        }

        public override void KeysEventPointedLine()
        {
            if ((KeyCode.Backspace.KeyDown()))
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
            MeshManager m = MeshManager.inst();
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

        public override void tool_pegi()
        {
            if (pegi.Click("Paint All with Brush Color"))
                m._Mesh.PaintAll(playtimeMesherSaveData.inst().brushConfig.color);

            pegi.toggle(ref playtimeMesherSaveData.inst().MakeVericesUniqueOnEdgeColoring, "Make Vertices Unique On coloring", 60); 
            pegi.newLine();

            cfg.brushConfig.ColorSliders_PEGI();
            pegi.newLine();
            if (pegi.Click("Colors AUTO for Detection"))
                m._Mesh.AutoSetVertColors();
            pegi.newLine();
        }

        public override void MouseEventPointedVertex()
        {
            MeshManager m = MeshManager.inst();

            BrushConfig bcf = playtimeMesherSaveData.inst().brushConfig;

            if (EditorInputManager.GetMouseButtonDown(1))
                m.pointedUV.vert.clearColor(playtimeMesherSaveData.inst().brushConfig.mask);

            if ((EditorInputManager.GetMouseButtonDown(0)))
            {
                if (EditorInputManager.getControlKey())
                {
                    foreach (UVpoint uvi in m.pointedUV.vert.uv)
                        uvi._color.From(playtimeMesherSaveData.inst().brushConfig.color, bcf.mask);
                }
                else
                    m.pointedUV._color.From(bcf.color, bcf.mask);

       
                m._Mesh.Dirty = true;
            }
        }

        public override void MouseEventPointedLine()
        {
            if (EditorInputManager.GetMouseButtonDown(0))  {
                BrushConfig bcf = cfg.brushConfig;

                UVpoint a = line.pnts[0];
                UVpoint b = line.pnts[1];

                a.vert.SetColorOnLine(bcf.color.ToColor(), bcf.mask, b.vert);//setColor(glob.colorSampler.color, glob.colorSampler.mask);
                b.vert.SetColorOnLine(bcf.color.ToColor(), bcf.mask, a.vert);
                m._Mesh.Dirty = true;
            }
        }

        public override void KeysEventPointedLine()
        {
            UVpoint a = line.pnts[0];
            UVpoint b = line.pnts[1];

            if (KeyCode.Alpha1.KeyDown())
                a.vert.FlipChanelOnLine(ColorChanel.R, b.vert);

            if (KeyCode.Alpha2.KeyDown())
                a.vert.FlipChanelOnLine(ColorChanel.G, b.vert);

            if (KeyCode.Alpha3.KeyDown())
                a.vert.FlipChanelOnLine(ColorChanel.B, b.vert);

            if (KeyCode.Alpha4.KeyDown())
                a.vert.FlipChanelOnLine(ColorChanel.A, b.vert);

        }

    }

    public class VertexShadowTool : MeshToolBase
    {
        public override MeshTool myTool { get { return MeshTool.VertexShadow; } }

        public override void MouseEventPointedVertex()
        {
            MeshManager m = MeshManager.inst();

            if (m.pointedUV == null) return;

            if ((EditorInputManager.GetMouseButtonDown(0)))
            {
                BrushConfig bcf = playtimeMesherSaveData.inst().brushConfig;
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
                return false;
            }
        }

        public override bool showVertices {
            get
            {
                return false;
            }
        }

        public static VertexAtlasTool inst;

        public VertexAtlasTool()
        {
            inst = this;
        }

        public override MeshTool myTool { get { return MeshTool.AtlasTexture; } }

        public override void tool_pegi()
        {
            pegi.write("Atlas Texture: ");
            pegi.edit(ref cfg.curAtlasTexture);
            pegi.newLine();
            if (m.selectedTris != null)
            {
                pegi.write("Selected tris uses Atlas Texture " + m.selectedTris.textureNo[0]);
                pegi.newLine();
            }
        }

        public override void MouseEventPointedTriangle()
        {
            MeshManager m = MeshManager.inst();
            if (EditorInputManager.GetMouseButtonDown(0))
            {
                m.pointedTris.textureNo[0] = playtimeMesherSaveData.inst().curAtlasTexture;
                m._Mesh.Dirty = true;
            }
        }

        public void SetAllTrianglesTextureTo(int no, int chanel) {

            List<trisDta> ts = m._Mesh.triangles;

            foreach (trisDta t in ts)
                t.textureNo[chanel] = no;

            m._Mesh.Dirty = true;
        }

    }

}