using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
using UnityEngine.SceneManagement;
using StoryTriggerData;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter {

    [ExecuteInEditMode]
    [Serializable]
    public class TileableAtlasingControllerPlugin : PainterManagerPluginBase
    {

        public static TileableAtlasingControllerPlugin inst;

        public override string ToString()
        {
            return "Tilable Atlasing";
        }

        public List<AtlasTextureCreator> atlases;

        public List<MaterialAtlases> atlasedMaterials;

        [SerializeField]
        protected bool showAtlasedMaterial;
        [SerializeField]
        protected bool showAtlases;

        [SerializeField]
        protected int browsedAtlas;

        public static bool putEdgesBetweenSubmeshes()
        {


            if (meshMGMT.target.isAtlased()) {
                "ATL_tex_Chanal:".edit(80,ref TriangleAtlasTool.inst.curAtlasChanel);

                if ("Auto Edge".Click().nl())
                {

                    editedMesh.tagTrianglesUnprocessed();
                    foreach (var t in editedMesh.triangles) if (!t.wasProcessed)
                        {

                            t.wasProcessed = true;

                            var ntris = t.GetNeighboringTrianglesUnprocessed();
                            foreach (var nt in ntris)
                                if (t.textureNo[TriangleAtlasTool.inst.curAtlasChanel] != nt.textureNo[TriangleAtlasTool.inst.curAtlasChanel])
                                {
                                    var ln = t.LineWith(nt);
                                    if (ln != null) VertexEdgeTool.putEdgeOnLine(ln);
                                    else Debug.Log("null line discoveredd");
                                }
                        }
                    editedMesh.dirty = true;

                }
            }
            return false;
        }

        public override void OnEnable() {

            inst = this;
            if (atlases == null)
                atlases = new List<AtlasTextureCreator>();

            if (atlasedMaterials == null)
                atlasedMaterials = new List<MaterialAtlases>();

            PlugIn_VertexEdgePEGI(putEdgesBetweenSubmeshes);

            PlugIn_CPUblitMethod(PaintTexture2D);
            

        }

        public override bool ConfigTab_PEGI()
        {
            bool changed = false;

            if (inspectedPainter.isAtlased())
            {

                "***** Selected Material Atlased *****".nl();
#if UNITY_EDITOR

                var m = inspectedPainter.getMesh();
                if (m != null && AssetDatabase.GetAssetPath(m).Length == 0)
                {
                    "Atlased Mesh is not saved".nl();
                    var n = m.name;
                    if ("Mesh Name".edit(80, ref n))
                        m.name = n;
                    if (icon.save.Click().nl())
                        inspectedPainter.SaveMesh();
                }

#endif


                var atlPlug = inspectedPainter.getPlugin<TileableAtlasingPainterPlugin>();

                if ("Undo Atlasing".Click())
                {
                    inspectedPainter.meshRenderer.sharedMaterials = atlPlug.preAtlasingMaterials;

                    if (atlPlug.preAtlasingMesh != null)
                        inspectedPainter.meshFilter.mesh = atlPlug.preAtlasingMesh;
                    inspectedPainter.savedEditableMesh = atlPlug.preAtlasingSavedMesh;

                    atlPlug.preAtlasingMaterials = null;
                    atlPlug.preAtlasingMesh = null;
                    inspectedPainter.meshRenderer.sharedMaterial.DisableKeyword(PainterConfig.UV_ATLASED);
                }

                if ("Not Atlased".Click().nl())
                {
                    atlPlug.preAtlasingMaterials = null;
                    inspectedPainter.meshRenderer.sharedMaterial.DisableKeyword(PainterConfig.UV_ATLASED);
                }

                pegi.newLine();

            }
            else if ("Atlased Materials".foldout(ref showAtlasedMaterial).nl())
            {
                showAtlases = false;
                changed |= atlasedMaterials.PEGI(ref inspectedPainter.selectedAtlasedMaterial, true).nl();
            }

            if ("Atlases".foldout(ref showAtlases))
            {

                if ((browsedAtlas > -1) && (browsedAtlas >= atlases.Count))
                    browsedAtlas = -1;

                pegi.newLine();

                if (browsedAtlas > -1)
                {
                    if (icon.Back.Click(25))
                        browsedAtlas = -1;
                    else
                        atlases[browsedAtlas].PEGI();
                }
                else
                {
                    pegi.newLine();
                    for (int i = 0; i < atlases.Count; i++)
                    {
                        if (icon.Delete.Click(25))
                            atlases.RemoveAt(i);
                        else
                        {
                            pegi.edit(ref atlases[i].name);
                            if (icon.Edit.Click(25).nl())
                                browsedAtlas = i;
                        }
                    }

                    if (icon.Add.Click(30))
                        atlases.Add(new AtlasTextureCreator("new"));

                }

            }

            return changed;

        }

        public static bool PaintTexture2D(StrokeVector stroke, float brushAlpha, ImageData image, BrushConfig bc, PlaytimePainter pntr) {
            var pl = pntr.getPlugin<TileableAtlasingPainterPlugin>();
            if (pl != null) return pl.PaintTexture2D(stroke, brushAlpha, image, bc, pntr);
            else return false;
        }
    }

    public class TriangleAtlasTool : MeshToolBase
    {

        public override string ToString() { return "triangle Atlas Textures"; }

        public override bool showLines
        {
            get
            {
                return false;
            }
        }

        public override bool showVerticesDefault {
            get {
                return true;
            }
        }

        static TriangleAtlasTool _inst;

        public int curAtlasTexture = 0;
        public int curAtlasChanel = 0;
        public bool atlasEdgeAsChanel2 = true;

        public static TriangleAtlasTool inst {
            get
            {
                if (_inst == null)
                {
                    var a = allTools;
                    return _inst;
                }
                return _inst;
            }
        }

        public TriangleAtlasTool()
        {
            _inst = this;
        }

        public override string tooltip
        {
            get
            {
                return "Select Texture Number and paint on triangles and lines. Texture an be selected with number keys, and sampled with Ctrl+LMB.";
            }
        }

        public override bool PEGI()
        {

            //"Edge Click as Chanel 2".toggle(ref atlasEdgeAsChanel2).nl();

            if (!inspectedPainter.material.isAtlased())
                ("Atlasing will work with custom shader that has tilable sampling from atlas. The material with such shader is not detected. " +
                    "But who am I to tell you what to do, I'm just a computer, a humble" +
                    " servant of a human race ... for now").writeWarning();

            "Atlas Texture: ".edit(ref curAtlasTexture).nl();
            "Atlas Chanel: ".edit(ref curAtlasChanel).nl();

            if (meshMGMT.selectedTris != null)
            {
                ("Selected tris uses Atlas Texture " + meshMGMT.selectedTris.textureNo[0]).nl();
            }

            pegi.writeHint("Cntrl + LMB -> Sample Texture Index");
            return false;
        }

        public override bool MouseEventPointedTriangle()
        {

            if (EditorInputManager.GetMouseButton(0))
            {
                

                if (EditorInputManager.getControlKey())
                    curAtlasTexture = (int)meshMGMT.pointedTris.textureNo[curAtlasChanel];
                else if (pointedTris.textureNo[curAtlasChanel] != curAtlasTexture)
                {
                    if (pointedTris.sameAsLastFrame)
                        return true;
                    pointedTris.textureNo[curAtlasChanel] = curAtlasTexture;
                    meshMGMT.edMesh.dirty = true;
                    return true;
                }

               

            }
            return false;
        }

        public override bool MouseEventPointedLine()
        {
            if (EditorInputManager.GetMouseButton(0) && !EditorInputManager.getControlKey())
            {

                if (pointedLine.sameAsLastFrame)
                    return true;

                foreach (var t in meshMGMT.pointedLine.getAllTriangles_USES_Tris_Listing())
                    if (t.textureNo[curAtlasChanel] != curAtlasTexture)
                    {
                        t.textureNo[curAtlasChanel] = curAtlasTexture;
                        meshMGMT.edMesh.dirty = true;
                    }
                return true;
            }
            return false;
        }

        public override bool MouseEventPointedVertex()
        {
            if (EditorInputManager.GetMouseButton(0))
            {

                if (pointedUV.sameAsLastFrame)
                    return true;

                foreach (var uv in meshMGMT.pointedUV.vert.uvpoints )
                    foreach (var t in uv.tris)
                    if (t.textureNo[curAtlasChanel] != curAtlasTexture) {
                        t.textureNo[curAtlasChanel] = curAtlasTexture;
                        meshMGMT.edMesh.dirty = true;
                    }
                return true;
            }
            return false;
        }

       

        public void SetAllTrianglesTextureTo(int no, int chanel)
        {

            foreach (trisDta t in editedMesh.triangles)
                t.textureNo[chanel] = no;

            meshMGMT.edMesh.dirty = true;
        }

        public void SetAllTrianglesTextureTo(int no, int chanel, int submesh)
        {

            foreach (trisDta t in editedMesh.triangles)
                if (t.submeshIndex == submesh)
                    t.textureNo[chanel] = no;

            meshMGMT.edMesh.dirty = true;
        }

        public override void KeysEventPointedTriangle()
        {


            int keyDown = Event.current.NumericKeyDown();

            if (keyDown != -1)
            {
                curAtlasTexture = keyDown;
                meshMGMT.pointedTris.textureNo[curAtlasChanel] = keyDown;
                meshMGMT.edMesh.dirty = true;
                if (!Application.isPlaying) Event.current.Use();
            }

        }

        public override stdEncoder Encode()
        {
            var cody = new stdEncoder();
            cody.Add("cat", curAtlasTexture);
            cody.Add("cac", curAtlasChanel);
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