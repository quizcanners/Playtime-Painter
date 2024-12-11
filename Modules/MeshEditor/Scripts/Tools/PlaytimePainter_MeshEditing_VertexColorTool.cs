using QuizCanners.Migration;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool.MeshEditing
{

    public class VertexColorTool : MeshToolBase
    {
        public override string StdTag => "t_vCol";

        public static VertexColorTool inst;

        public override bool ShowVertices => _detectionMode == DetectionMode.Points;
        public override bool ShowLines => _detectionMode == DetectionMode.Lines;
        public override bool ShowTriangles => _detectionMode == DetectionMode.Triangles;



        private bool _isPaintingWithRadius;
        private float _radiusOfPainting;
        private float _alpha = 1;

        public override Color VertexColor => Color.white;

        public int selectedSubMesh;

        private bool constantUpdateOnGroupColors;

        #region Inspector
        public override string Tooltip => (" Vertex Color {0} 1. Make sure mesh Profile has Color enabled {0}" +
                                          "2. Only if shader outputs vertex color, changes will be visible. " +
                                          "1234 on Line - apply RGBA for Border.").F(pegi.EnvironmentNl);

        public override string ToString() => "vertex Color";

        public override void Inspect()
        {
            var changed = pegi.ChangeTrackStart();

            var em = EditedMesh;
            var br = Painter.Data.Brush;
            var col = br.Color;
            var p = InspectedPainter;

            InspectDetectionMode();
            pegi.Nl();

            //"Mode".PegiLabel(50).Edit_Enum(ref _detectionMode).Nl();

            "Make Vertex Unique On Paint".PL().ToggleIcon(ref Painter.Data.makeVerticesUniqueOnEdgeColoring).Nl();


            "Paint Radius".PL().ToggleIcon(ref _isPaintingWithRadius, hideTextWhenTrue: true);
            if (_isPaintingWithRadius)
                "Radius".ConstL().Edit(ref _radiusOfPainting, min: em.averageSize * 0.001f, max: em.averageSize);

            pegi.Nl();

            "Flow".ConstL().Edit_01(ref _alpha).Nl();

            if (em.subMeshCount > 1)
            {

                var cnt = em.subMeshCount;
                var mats = InspectedPainter.Materials;

                var nms = new string[cnt];
                for (var i = 0; i < cnt; i++)
                    nms[i] = "{0}: {1}".F(i, mats.TryGet(i));

                "Color Sub Mesh".ConstL().Select(ref selectedSubMesh, nms);

                if (selectedSubMesh < em.subMeshCount && "Apply".PL().Click())
                    em.ColorSubMesh(selectedSubMesh, col);

                pegi.Nl();

            }

            br.ColorSliders();
            pegi.Nl();

            if (p.MeshProfile == null)
                "No Mesh Packaging profile selected".PL().WriteWarning();
            else
            {
                if (!p.MeshProfile.UsesColor)
                    "Selected Mesh Profile does not appear to be using Color".PL().WriteWarning();
                if (!p.MeshProfile.WritesColor)
                    "Selected Mesh Profile doesn't write to Color.".PL().Write_Hint();
            }


            if (("Paint All with Brush Color").PL().Click().Nl())
                em.PaintAll(br.Color);

            "Submeshes".PL(style: pegi.Styles.ListLabel).Nl();

            for (int i = 0; i <= em.maxGroupIndex; i++)
            {

                "{0}:".F(i).PL(20).Write();

                var c = em.groupColors[i];

                if (Icon.Refresh.Click("Get Actual color") || c == default)
                {
                    c = GetGroupColor(i);
                    em.groupColors[i] = c;
                }

                if (pegi.Edit(ref c))
                {
                    em.groupColors[i] = c;
                    if (constantUpdateOnGroupColors)
                        SetGroupColor(i, c);
                }

                if (!constantUpdateOnGroupColors && Icon.Clear.Click("Fill group {0} "))
                    SetGroupColor(i, c);

            }

            pegi.Nl();

            "Recolor group On Edit".PL().ToggleIcon(ref constantUpdateOnGroupColors);

            pegi.FullWindow.DocumentationClickOpen(() => ("If mesh has submeshes he will have a couple of groups. This can be used to change their colors individually." +
                                                                     "After changing color of the group, you can click on the brush to the right to apply the color." +
                                                                     "Alternatively, you can enable Recolor_Group_On_Edit so that change will be applied instantly."));

            pegi.Nl();

            "Paint Extrusion [-1 1] To Alpha".PL().ClickConfirm("PwAmb").OnChanged(PaintExtrusionToAlpha).Nl();

        }

        private Color GetGroupColor(int index)
        {

            foreach (var p in EditedMesh.meshPoints)
                foreach (var u in p.vertices)
                    if (u.groupIndex == index)
                        return u.color;

            return Color.white;
        }

        private void SetGroupColor(int index, Color col)
        {
            foreach (var p in EditedMesh.meshPoints)
                foreach (var v in p.vertices)
                    if (v.groupIndex == index)
                        v.color = col;

            EditedMesh.dirtyColor = true;

        }

        #endregion

        private void PaintExtrusionToAlpha() 
        {
            foreach (var p in EditedMesh.meshPoints) 
            {
                var amb = p.ExtrusionLevelOneMinusOne();
                p.SetColor(ColorMask.A, new Color(amb,amb,amb,amb));
            }

            EditedMesh.dirtyColor = true;
        }

        private void PaintRadius(PainterMesh.MeshPoint point) 
        {
            var pos1 = point.WorldPos;
            var bcf = GlobalBrush;
            var mask = bcf.mask;

            foreach (var p in MeshEditorManager.editedMesh.meshPoints)
            {
                var pos2 = p.WorldPos;
                var dist = Vector3.Distance(pos1, pos2);

                if (dist > _radiusOfPainting)
                    continue;

                float alpha = dist * _alpha / _radiusOfPainting;

                foreach (var uvi in p.vertices)
                    mask.SetValuesOn(ref uvi.color, source: Painter.Data.Brush.Color, alpha: alpha);
            }

            EditedMesh.dirtyColor = true;
        }

        public override bool MouseEventPointedVertex()
        {
            var bcf = GlobalBrush;

            //if (EditorInputManager.GetMouseButtonDown(1))
            //  m.pointedUV.vert.clearColor(cfg.brushConfig.mask);

            if ((PlaytimePainter_EditorInputManager.GetMouseButtonDown(0)))
            {
                if (PlaytimePainter_EditorInputManager.Control)
                    bcf.mask.SetValuesOn(ref MeshEditorManager.PointedUv.color, bcf.Color, alpha: _alpha);
                else
                {
                    foreach (var uvi in MeshEditorManager.PointedUv.meshPoint.vertices)
                        bcf.mask.SetValuesOn(ref uvi.color, Painter.Data.Brush.Color, alpha: _alpha);

                    if (_isPaintingWithRadius)
                        PaintRadius(MeshEditorManager.PointedUv.meshPoint);
                }

                EditedMesh.dirtyColor = true;

            }

            return false;
        }

        public override bool MouseEventPointedLine()
        {
            if (!PlaytimePainter_EditorInputManager.GetMouseButton(0)) return false;

            if (PointedLine.SameAsLastFrame)
                return true;

            var bcf = Painter.Data.Brush;

            var a = PointedLine.vertexes[0];
            var b = PointedLine.vertexes[1];

            var c = bcf.Color;

            a.meshPoint.SetColorOnLine(c, bcf.mask, b.meshPoint);//setColor(glob.colorSampler.color, glob.colorSampler.mask);
            b.meshPoint.SetColorOnLine(c, bcf.mask, a.meshPoint);

            if (_isPaintingWithRadius)
            {
                PaintRadius(a.meshPoint);
                PaintRadius(b.meshPoint);
            }

            EditedMesh.dirtyColor = true;
            return true;
        }

        public override bool MouseEventPointedTriangle()
        {
            if (!PlaytimePainter_EditorInputManager.GetMouseButton(0)) return false;

            if (PointedTriangle.SameAsLastFrame)
                return true;

            var bcf = Painter.Data.Brush;

            var c = bcf.Color;

            foreach (var u in PointedTriangle.vertexes)
                foreach (var vuv in u.meshPoint.vertices)
                    bcf.mask.SetValuesOn(ref vuv.color, c, alpha: _alpha);

            if (_isPaintingWithRadius)
                foreach (var u in PointedTriangle.vertexes)
                    PaintRadius(u.meshPoint);
                
            EditedMesh.dirtyColor = true;
            return true;
        }

        public override void KeysEventPointedLine()
        {
            var a = PointedLine.vertexes[0];
            var b = PointedLine.vertexes[1];

            var ind = Event.current.NumericKeyDown();

            if ((ind > 0) && (ind < 5))
            {
                a.meshPoint.FlipChanelOnLine((ColorChanel)(ind - 1), b.meshPoint);
                EditedMesh.Dirty = true;
                Event.current.Use();
            }

        }

        public VertexColorTool()
        {
            inst = this;
        }

        #region Encode & Decode

        public override CfgEncoder Encode() => new CfgEncoder()
            .Add_IfNotZero("sm", selectedSubMesh);

        public override void DecodeTag(string key, CfgData data)
        {

            switch (key)
            {
                case "sm": selectedSubMesh = data.ToInt(); break;
            }
        }

        #endregion

    }

}