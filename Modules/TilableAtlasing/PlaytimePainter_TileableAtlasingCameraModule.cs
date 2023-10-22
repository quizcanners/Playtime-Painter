using System;
using QuizCanners.Inspect;
using PainterTool.ComponentModules;
using PainterTool.MeshEditing;
using QuizCanners.Utils;
using UnityEngine;
using QuizCanners.Migration;

namespace PainterTool
{

    namespace CameraModules
    {
        [TaggedTypes.Tag(CLASS_KEY)]
        internal class TileableAtlasingCameraModule : CameraModuleBase, IMeshToolPlugin, IPainterManagerModuleBrush
        {
            private const string CLASS_KEY = "TilAtlCntrl";
            public override string ClassTag => CLASS_KEY;

            public static TileableAtlasingCameraModule inst;

            public override string ToString() => "Tilable Atlasing";
            
            #region Inspector

            protected int inspectedAtlas;

            public void MeshToolInspection(MeshToolBase currentTool)
            {

                if (currentTool is VertexEdgeTool && MeshPainting.target.IsAtlased())
                {
                    "ATL_tex_Chanal:".PegiLabel(80).Edit(ref TriangleAtlasTool.Inst.curAtlasChanel);

                    if ("Auto Edge".PegiLabel().Click().Nl())
                    {

                        EditedMesh.TagTrianglesUnprocessed();
                        foreach (var t in EditedMesh.triangles)
                            if (!t.wasProcessed)
                            {

                                t.wasProcessed = true;

                                var ntris = t.GetNeighboringTrianglesUnprocessed();
                                foreach (var nt in ntris)
                                    if (Mathf.Approximately(t.textureNo[TriangleAtlasTool.Inst.curAtlasChanel],
                                        nt.textureNo[TriangleAtlasTool.Inst.curAtlasChanel]) == false)
                                    {
                                        var ln = t.LineWith(nt);
                                        if (ln != null) VertexEdgeTool.PutEdgeOnLine(ln);
                                        else Debug.Log("null line discoveredd");
                                    }
                            }

                        EditedMesh.Dirty = true;

                    }
                }
            }

            public override void Enable() => inst = this;

            private readonly pegi.EnterExitContext context = new(); 

            public void Inspect()
            {
                bool changed = false;

                var p = InspectedPainter;

                if (p.IsAtlased())
                {

                    "***** Selected Material Atlased *****".PegiLabel().Nl();
#if UNITY_EDITOR

                    var m = p.GetMesh();
                    if (m && UnityEditor.AssetDatabase.GetAssetPath(m).Length == 0)
                    {
                        "Atlased Mesh is not saved".PegiLabel().Nl();
                        var n = m.name;
                        if ("Mesh Name".PegiLabel(80).Edit(ref n))
                            m.name = n;
                        Icon.Save.Click(() => p.SaveMesh()).Nl();
                    }
#endif


                    var atlPlug = p.GetModule<TileableAtlasingComponentModule>();

                    if ("Undo Atlasing".PegiLabel().Click())
                    {
                        p.meshRenderer.sharedMaterials = atlPlug.preAtlasingMaterials.ToArray();

                        if (atlPlug.preAtlasingMesh)
                            p.Mesh = atlPlug.preAtlasingMesh;
                        p.SavedEditableMesh = atlPlug.preAtlasingSavedMesh;

                        atlPlug.preAtlasingMaterials = null;
                        atlPlug.preAtlasingMesh = null;
                        p.meshRenderer.sharedMaterial.DisableKeyword(PainterShaderVariables.UV_ATLASED);
                    }

                    if ("Not Atlased".PegiLabel().Click().Nl())
                    {
                        atlPlug.preAtlasingMaterials = null;
                        p.meshRenderer.sharedMaterial.DisableKeyword(PainterShaderVariables.UV_ATLASED);
                    }

                    pegi.Nl();

                }


                using (context.StartContext())
                {

                    if (p)
                        "Atlased Materials".PegiLabel()
                            .Enter_List(Painter.Data.atlasedMaterials, ref p.selectedAtlasedMaterial)
                            .Nl();

                    "Atlases".PegiLabel().Enter_List(Painter.Data.atlases, ref inspectedAtlas).Nl();
                }
                if (changed)
                    Painter.Data.SetToDirty();

            }

            #endregion

            public void PaintPixelsInRam(Painter.Command.Base command)
            {
                var painter= command.TryGetPainter(); 

                if (!painter)
                    return;

                painter.GetModule<TileableAtlasingComponentModule>()?.PaintTexture2D(command); 
            }

            public bool IsA3DBrush(PainterComponent painter, Brush bc, ref bool overrideOther) => false;

            public void PaintRenderTextureUvSpace(Painter.Command.Base command)
            {
            }

            // public bool NeedsGrid(PlaytimePainter p) => false;

            public Shader GetPreviewShader(PainterComponent p) => null;

            public Shader GetBrushShaderDoubleBuffer(PainterComponent p) => null;

            public Shader GetBrushShaderSingleBuffer(PainterComponent p) => null;

            public void BrushConfigPEGI(Brush br) { }

            public bool IsEnabledFor(PainterComponent painter, TextureMeta id, Brush cfg) =>
                painter.IsAtlased() && !id.TargetIsRenderTexture();

        }
    }

    public class TriangleAtlasTool : MeshToolBase
    {

        public override string ToString() => "triangle Atlas Textures"; 

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
                    InitIfNotInited();
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

            //"Edge Click as Chanel 2".PegiLabel().toggle(ref atlasEdgeAsChanel2).nl();

            if (!InspectedPainter.Material.IsAtlased())
                ("Atlasing will work with custom shader that has tilable sampling from atlas. The material with such shader is not detected. " +
                    "But who am I to tell you what to do, I'm just a computer, a humble" +
                    " servant of a human race ... for now").PegiLabel().WriteWarning();

            "Atlas Texture: ".PegiLabel().Edit(ref _curAtlasTexture).Nl();
            "Atlas Chanel: ".PegiLabel().Edit(ref curAtlasChanel).Nl();

            if (MeshEditorManager.SelectedTriangle != null)
                ("Selected triangles uses Atlas Texture " + MeshEditorManager.SelectedTriangle.textureNo[0]).PegiLabel().Nl();
            
            "Cntrl + LMB -> Sample Texture Index".PegiLabel().Write_Hint();
        }

        #endregion

        public override bool MouseEventPointedTriangle()
        {
            if (PlaytimePainter_EditorInputManager.GetMouseButton(0))
            {
                if (PlaytimePainter_EditorInputManager.Control)
                    _curAtlasTexture = (int)MeshEditorManager.PointedTriangle.textureNo[curAtlasChanel];
                else if (Mathf.Approximately(PointedTriangle.textureNo[curAtlasChanel], _curAtlasTexture) == false)
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
            if (PlaytimePainter_EditorInputManager.GetMouseButton(0) && !PlaytimePainter_EditorInputManager.Control)
            {

                if (PointedLine.SameAsLastFrame)
                    return true;

                foreach (var t in MeshEditorManager.PointedLine.TryGetBothTriangles())
                    if (Mathf.Approximately(t.textureNo[curAtlasChanel], _curAtlasTexture) == false)
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
            if (PlaytimePainter_EditorInputManager.GetMouseButton(0))
            {

                if (PointedUv.SameAsLastFrame)
                    return true;

                foreach (var uv in MeshEditorManager.PointedUv.meshPoint.vertices )
                    foreach (var t in uv.triangles)
                    if ( Mathf.Approximately(t.textureNo[curAtlasChanel], _curAtlasTexture) == false) {
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
                MeshEditorManager.PointedTriangle.textureNo[curAtlasChanel] = keyDown;
                EditedMesh.Dirty = true;
                if (!Application.isPlaying) Event.current.Use();
            }
        }

        #region Encode & Decode
        public override CfgEncoder Encode() => new CfgEncoder()
            .Add("cat", _curAtlasTexture)
            .Add("cac", curAtlasChanel);
   
        public override void DecodeTag(string key, CfgData data)
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