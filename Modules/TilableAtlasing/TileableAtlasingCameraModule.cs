using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using PlaytimePainter.ComponentModules;
using PlaytimePainter.MeshEditing;
using QuizCanners.Utils;
using UnityEngine;
using QuizCanners.CfgDecode;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter
{

    namespace CameraModules
    {
        [TaggedType(tag)]
        public class TileableAtlasingCameraModule : CameraModuleBase, IMeshToolPlugin, IPainterManagerModuleBrush
        {
            const string tag = "TilAtlCntrl";
            public override string ClassTag => tag;

            public static TileableAtlasingCameraModule inst;

            public override string NameForDisplayPEGI() => "Tilable Atlasing";
            
            #region Inspector

            [SerializeField] protected int inspectedAtlas;

            public bool MeshToolInspection(MeshToolBase currentTool)
            {

                if (currentTool is VertexEdgeTool && MeshEditorManager.target.IsAtlased())
                {
                    "ATL_tex_Chanal:".edit(80, ref TriangleAtlasTool.Inst.curAtlasChanel);

                    if ("Auto Edge".Click().nl())
                    {

                        EditedMesh.TagTrianglesUnprocessed();
                        foreach (var t in EditedMesh.triangles)
                            if (!t.wasProcessed)
                            {

                                t.wasProcessed = true;

                                var ntris = t.GetNeighboringTrianglesUnprocessed();
                                foreach (var nt in ntris)
                                    if (t.textureNo[TriangleAtlasTool.Inst.curAtlasChanel] !=
                                        nt.textureNo[TriangleAtlasTool.Inst.curAtlasChanel])
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

            public override void Enable() => inst = this;

            int InspectedItems = -1;

            public void Inspect()
            {
                bool changed = false;

                var p = InspectedPainter;

                if (p.IsAtlased())
                {

                    "***** Selected Material Atlased *****".nl();
#if UNITY_EDITOR

                    var m = p.GetMesh();
                    if (m && AssetDatabase.GetAssetPath(m).Length == 0)
                    {
                        "Atlased Mesh is not saved".nl();
                        var n = m.name;
                        if ("Mesh Name".edit(80, ref n))
                            m.name = n;
                        if (icon.Save.Click().nl())
                            p.SaveMesh();
                    }
#endif


                    var atlPlug = p.GetModule<TileableAtlasingComponentModule>();

                    if ("Undo Atlasing".Click())
                    {
                        p.meshRenderer.sharedMaterials = atlPlug.preAtlasingMaterials.ToArray();

                        if (atlPlug.preAtlasingMesh)
                            p.Mesh = atlPlug.preAtlasingMesh;
                        p.SavedEditableMesh = atlPlug.preAtlasingSavedMesh;

                        atlPlug.preAtlasingMaterials = null;
                        atlPlug.preAtlasingMesh = null;
                        p.meshRenderer.sharedMaterial.DisableKeyword(PainterShaderVariables.UV_ATLASED);
                    }

                    if ("Not Atlased".Click().nl())
                    {
                        atlPlug.preAtlasingMaterials = null;
                        p.meshRenderer.sharedMaterial.DisableKeyword(PainterShaderVariables.UV_ATLASED);
                    }

                    pegi.nl();

                }

                if (p)
                    "Atlased Materials"
                        .enter_List(ref Cfg.atlasedMaterials, ref p.selectedAtlasedMaterial, ref InspectedItems, 0)
                        .nl(ref changed);

                "Atlases".enter_List(ref Cfg.atlases, ref inspectedAtlas, ref InspectedItems, 1).nl(ref changed);

                if (changed)
                    Cfg.SetToDirty();

            }

            #endregion

            public void PaintPixelsInRam(PaintCommand.UV command)
            {
                var painter= command.TryGetPainter(); 

                if (!painter)
                    return;

                painter.GetModule<TileableAtlasingComponentModule>()?.PaintTexture2D(command); 
            }

            public bool IsA3DBrush(PlaytimePainter painter, Brush bc, ref bool overrideOther) => false;

            public void PaintRenderTextureUvSpace(PaintCommand.UV command)
            {
            }

            // public bool NeedsGrid(PlaytimePainter p) => false;

            public Shader GetPreviewShader(PlaytimePainter p) => null;

            public Shader GetBrushShaderDoubleBuffer(PlaytimePainter p) => null;

            public Shader GetBrushShaderSingleBuffer(PlaytimePainter p) => null;

            public bool BrushConfigPEGI(Brush br) => false;

            public bool IsEnabledFor(PlaytimePainter painter, TextureMeta id, Brush cfg) =>
                painter.IsAtlased() && !id.TargetIsRenderTexture();

        }
    }

    public class TriangleAtlasTool : MeshToolBase
    {

        public override string NameForDisplayPEGI()=> "triangle Atlas Textures"; 

        public override bool ShowLines=> false;

        public override bool ShowVertices => true;

        private static TriangleAtlasTool _inst;

        private int _curAtlasTexture;
        public int curAtlasChanel;
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

        #region Inspecotr
        public override string Tooltip => "Select Texture Number and paint on triangles and lines.  Texture can be selected with number keys, and sampled with Ctrl+LMB." + Environment.NewLine 
                                        + "You need to use a special shader that has _isAtlased option";
        
       public override void Inspect()
        {

            //"Edge Click as Chanel 2".toggle(ref atlasEdgeAsChanel2).nl();

            if (!InspectedPainter.Material.IsAtlased())
                ("Atlasing will work with custom shader that has tilable sampling from atlas. The material with such shader is not detected. " +
                    "But who am I to tell you what to do, I'm just a computer, a humble" +
                    " servant of a human race ... for now").writeWarning();

            "Atlas Texture: ".edit(ref _curAtlasTexture).nl();
            "Atlas Chanel: ".edit(ref curAtlasChanel).nl();

            if (MeshMGMT.SelectedTriangle != null)
                ("Selected triangles uses Atlas Texture " + MeshMGMT.SelectedTriangle.textureNo[0]).nl();
            
            "Cntrl + LMB -> Sample Texture Index".writeHint();
        }

        #endregion

        public override bool MouseEventPointedTriangle()
        {
            if (EditorInputManager.GetMouseButton(0))
            {
                if (EditorInputManager.Control)
                    _curAtlasTexture = (int)MeshMGMT.PointedTriangle.textureNo[curAtlasChanel];
                else if (PointedTriangle.textureNo[curAtlasChanel] != _curAtlasTexture)
                {
                    if (PointedTriangle.SameAsLastFrame)
                        return true;
                    PointedTriangle.textureNo[curAtlasChanel] = _curAtlasTexture;
                    EditedMesh.Dirty = true;
                    return true;
                }
            }
            return false;
        }

        public override bool MouseEventPointedLine()
        {
            if (EditorInputManager.GetMouseButton(0) && !EditorInputManager.Control)
            {

                if (PointedLine.SameAsLastFrame)
                    return true;

                foreach (var t in MeshMGMT.PointedLine.GetAllTriangles())
                    if (t.textureNo[curAtlasChanel] != _curAtlasTexture)
                    {
                        t.textureNo[curAtlasChanel] = _curAtlasTexture;
                        EditedMesh.Dirty = true;
                    }
                return true;
            }
            return false;
        }

        public override bool MouseEventPointedVertex()
        {
            if (EditorInputManager.GetMouseButton(0))
            {

                if (PointedUv.SameAsLastFrame)
                    return true;

                foreach (var uv in MeshMGMT.PointedUv.meshPoint.vertices )
                    foreach (var t in uv.triangles)
                    if (t.textureNo[curAtlasChanel] != _curAtlasTexture) {
                        t.textureNo[curAtlasChanel] = _curAtlasTexture;
                        EditedMesh.Dirty = true;
                    }
                return true;
            }
            return false;
        }

        public void SetAllTrianglesTextureTo(int no, int chanel)
        {

            foreach (PainterMesh.Triangle t in EditedMesh.triangles)
                t.textureNo[chanel] = no;

            EditedMesh.Dirty = true;
        }

        public void SetAllTrianglesTextureTo(int no, int chanel, int submesh)
        {

            foreach (PainterMesh.Triangle t in EditedMesh.triangles)
                if (t.subMeshIndex == submesh)
                    t.textureNo[chanel] = no;

            EditedMesh.Dirty = true;
        }

        public override void KeysEventPointedTriangle()
        {
            int keyDown = Event.current.NumericKeyDown();

            if (keyDown != -1)
            {
                _curAtlasTexture = keyDown;
                MeshMGMT.PointedTriangle.textureNo[curAtlasChanel] = keyDown;
                EditedMesh.Dirty = true;
                if (!Application.isPlaying) Event.current.Use();
            }
        }

        #region Encode & Decode
        public override CfgEncoder Encode() => new CfgEncoder()
            .Add("cat", _curAtlasTexture)
            .Add("cac", curAtlasChanel);
   
        public override void Decode(string key, CfgData data)
        {
            switch (key)
            {
                case "cat": data.ToInt(ref _curAtlasTexture); break;
                case "cac": data.ToInt(ref curAtlasChanel); break;
            }
        }
        #endregion
    }

}