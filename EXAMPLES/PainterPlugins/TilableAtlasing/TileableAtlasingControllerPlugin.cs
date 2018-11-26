using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
using System;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter {

    [TaggedType(tag)]
    [ExecuteInEditMode]
    [Serializable]
    public class TileableAtlasingControllerPlugin : PainterManagerPluginBase
    {
        const string tag = "TilAtlCntrl";
        public override string ClassTag => tag;


        public static TileableAtlasingControllerPlugin inst;

        public override string ToString()
        {
            return "Tilable Atlasing";
        }

        public List<AtlasTextureCreator> atlases;

        public List<MaterialAtlases> atlasedMaterials;



        [SerializeField]
        protected int inspectedAtlas;
#if PEGI
        public static bool PutEdgesBetweenSubmeshes()
        {


            if (MeshMGMT.target.IsAtlased()) {
                "ATL_tex_Chanal:".edit(80,ref TriangleAtlasTool.Inst.curAtlasChanel);

                if ("Auto Edge".Click().nl())
                {

                    EditedMesh.TagTrianglesUnprocessed();
                    foreach (var t in EditedMesh.triangles) if (!t.wasProcessed)
                        {

                            t.wasProcessed = true;

                            var ntris = t.GetNeighboringTrianglesUnprocessed();
                            foreach (var nt in ntris)
                                if (t.textureNo[TriangleAtlasTool.Inst.curAtlasChanel] != nt.textureNo[TriangleAtlasTool.Inst.curAtlasChanel])
                                {
                                    var ln = t.LineWith(nt);
                                    if (ln != null) VertexEdgeTool.PutEdgeOnLine(ln);
                                    else Debug.Log("null line discoveredd");
                                }
                        }
                    EditedMesh.Dirty = true;

                }
            }
            return false;
        }
#endif
        public override void OnEnable() {

            inst = this;
            if (atlases == null)
                atlases = new List<AtlasTextureCreator>();

            if (atlasedMaterials == null)
                atlasedMaterials = new List<MaterialAtlases>();
            #if PEGI
            PlugIn_VertexEdgePEGI(PutEdgesBetweenSubmeshes);
#endif
            PlugIn_CPUblitMethod(PaintTexture2D);
            

        }

#if PEGI

        int InspectedStuff = -1;

        public override bool ConfigTab_PEGI()
        {
            bool changed = false;

            if (InspectedPainter.IsAtlased())
            {

                "***** Selected Material Atlased *****".nl();
#if UNITY_EDITOR

                var m = InspectedPainter.GetMesh();
                if (m != null && AssetDatabase.GetAssetPath(m).Length == 0)
                {
                    "Atlased Mesh is not saved".nl();
                    var n = m.name;
                    if ("Mesh Name".edit(80, ref n))
                        m.name = n;
                    if (icon.Save.Click().nl())
                        InspectedPainter.SaveMesh();
                }

#endif


                var atlPlug = InspectedPainter.GetPlugin<TileableAtlasingPainterPlugin>();

                if ("Undo Atlasing".Click())
                {
                    InspectedPainter.meshRenderer.sharedMaterials = atlPlug.preAtlasingMaterials;

                    if (atlPlug.preAtlasingMesh != null)
                        InspectedPainter.meshFilter.mesh = atlPlug.preAtlasingMesh;
                    InspectedPainter.SavedEditableMesh = atlPlug.preAtlasingSavedMesh;

                    atlPlug.preAtlasingMaterials = null;
                    atlPlug.preAtlasingMesh = null;
                    InspectedPainter.meshRenderer.sharedMaterial.DisableKeyword(PainterDataAndConfig.UV_ATLASED);
                }

                if ("Not Atlased".Click().nl())
                {
                    atlPlug.preAtlasingMaterials = null;
                    InspectedPainter.meshRenderer.sharedMaterial.DisableKeyword(PainterDataAndConfig.UV_ATLASED);
                }

                pegi.newLine();

            }

            changed |= "Atlased Materials".enter_List(ref atlasedMaterials, ref InspectedPainter.selectedAtlasedMaterial, ref InspectedStuff, 0).nl();

            changed |= "Atlases".enter_List(ref atlases, ref inspectedAtlas, ref InspectedStuff, 1).nl();

            return changed;

        }

#endif

        public static bool PaintTexture2D(StrokeVector stroke, float brushAlpha, ImageData image, BrushConfig bc, PlaytimePainter pntr) {
            var pl = pntr.GetPlugin<TileableAtlasingPainterPlugin>();
            if (pl != null) return pl.PaintTexture2D(stroke, brushAlpha, image, bc, pntr);
            else return false;
        }
    }

    public class TriangleAtlasTool : MeshToolBase
    {

        public override string ToString() { return "triangle Atlas Textures"; }

        public override bool ShowLines
        {
            get
            {
                return false;
            }
        }

        public override bool ShowVerticesDefault {
            get {
                return true;
            }
        }

        static TriangleAtlasTool _inst;

        public int curAtlasTexture = 0;
        public int curAtlasChanel = 0;
        public bool atlasEdgeAsChanel2 = true;

        public static TriangleAtlasTool Inst {
            get
            {
                if (_inst == null)
                {
                    var a = AllTools;
                    return _inst;
                }
                return _inst;
            }
        }

        public TriangleAtlasTool()
        {
            _inst = this;
        }

        public override string Tooltip
        {
            get
            {
                return "Select Texture Number and paint on triangles and lines. Texture an be selected with number keys, and sampled with Ctrl+LMB.";
            }
        }
#if PEGI
        public override bool Inspect()
        {

            //"Edge Click as Chanel 2".toggle(ref atlasEdgeAsChanel2).nl();

            if (!InspectedPainter.Material.IsAtlased())
                ("Atlasing will work with custom shader that has tilable sampling from atlas. The material with such shader is not detected. " +
                    "But who am I to tell you what to do, I'm just a computer, a humble" +
                    " servant of a human race ... for now").writeWarning();

            "Atlas Texture: ".edit(ref curAtlasTexture).nl();
            "Atlas Chanel: ".edit(ref curAtlasChanel).nl();

            if (MeshMGMT.SelectedTris != null)
            {
                ("Selected tris uses Atlas Texture " + MeshMGMT.SelectedTris.textureNo[0]).nl();
            }

            pegi.writeHint("Cntrl + LMB -> Sample Texture Index");
            return false;
        }
#endif
        public override bool MouseEventPointedTriangle()
        {

            if (EditorInputManager.GetMouseButton(0))
            {
                

                if (EditorInputManager.getControlKey())
                    curAtlasTexture = (int)MeshMGMT.PointedTris.textureNo[curAtlasChanel];
                else if (PointedTris.textureNo[curAtlasChanel] != curAtlasTexture)
                {
                    if (PointedTris.SameAsLastFrame)
                        return true;
                    PointedTris.textureNo[curAtlasChanel] = curAtlasTexture;
                    MeshMGMT.edMesh.Dirty = true;
                    return true;
                }

               

            }
            return false;
        }

        public override bool MouseEventPointedLine()
        {
            if (EditorInputManager.GetMouseButton(0) && !EditorInputManager.getControlKey())
            {

                if (PointedLine.SameAsLastFrame)
                    return true;

                foreach (var t in MeshMGMT.PointedLine.GetAllTriangles_USES_Tris_Listing())
                    if (t.textureNo[curAtlasChanel] != curAtlasTexture)
                    {
                        t.textureNo[curAtlasChanel] = curAtlasTexture;
                        MeshMGMT.edMesh.Dirty = true;
                    }
                return true;
            }
            return false;
        }

        public override bool MouseEventPointedVertex()
        {
            if (EditorInputManager.GetMouseButton(0))
            {

                if (PointedUV.SameAsLastFrame)
                    return true;

                foreach (var uv in MeshMGMT.PointedUV.meshPoint.uvpoints )
                    foreach (var t in uv.tris)
                    if (t.textureNo[curAtlasChanel] != curAtlasTexture) {
                        t.textureNo[curAtlasChanel] = curAtlasTexture;
                        MeshMGMT.edMesh.Dirty = true;
                    }
                return true;
            }
            return false;
        }

       

        public void SetAllTrianglesTextureTo(int no, int chanel)
        {

            foreach (Triangle t in EditedMesh.triangles)
                t.textureNo[chanel] = no;

            MeshMGMT.edMesh.Dirty = true;
        }

        public void SetAllTrianglesTextureTo(int no, int chanel, int submesh)
        {

            foreach (Triangle t in EditedMesh.triangles)
                if (t.submeshIndex == submesh)
                    t.textureNo[chanel] = no;

            MeshMGMT.edMesh.Dirty = true;
        }

        public override void KeysEventPointedTriangle()
        {


            int keyDown = Event.current.NumericKeyDown();

            if (keyDown != -1)
            {
                curAtlasTexture = keyDown;
                MeshMGMT.PointedTris.textureNo[curAtlasChanel] = keyDown;
                MeshMGMT.edMesh.Dirty = true;
                if (!Application.isPlaying) Event.current.Use();
            }

        }

        public override StdEncoder Encode()
        {
            var cody = new StdEncoder()
            .Add("cat", curAtlasTexture)
            .Add("cac", curAtlasChanel);
           // cody.Add("aec2", atlasEdgeAsChanel2);
            return cody;
        }

        public override bool Decode(string tag, string data)
        {
            switch (tag)
            {
                case "cat": curAtlasTexture = data.ToInt(); break;
                case "cac": curAtlasChanel = data.ToInt(); break;
               // case "aec2": atlasEdgeAsChanel2 = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
    }

}