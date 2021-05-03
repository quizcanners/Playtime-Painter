using QuizCanners.Inspect;
using QuizCanners.Utils;

namespace PlaytimePainter.MeshEditing
{
    public class VertexShadowTool : MeshToolBase {
        public override string NameForDisplayPEGI()=> "vertex Shadow";

        public override bool MouseEventPointedVertex() {
           
            if ((EditorInputManager.GetMouseButton(0))) {
                if (PointedUv.SameAsLastFrame)
                    return true;
                var bcf = Cfg.Brush;
                //bcf.colorLinear.ToV4(ref PointedVertex.shadowBake, bcf.mask);
                bcf.mask.SetValuesOn(ref PointedVertex.shadowBake, bcf.Color);
                EditedMesh.Dirty = true;
                return true;
            }
            return false;
        }

        public override bool MouseEventPointedTriangle()
        {
            if ((EditorInputManager.GetMouseButton(0)))
            {
                if (PointedTriangle.SameAsLastFrame)
                    return true;
                Brush bcf = Cfg.Brush;
                foreach (var uv in PointedTriangle.vertexes)
                //bcf.colorLinear.ToV4(ref uv.meshPoint.shadowBake, bcf.mask);
                bcf.mask.SetValuesOn(ref uv.meshPoint.shadowBake, bcf.Color);
                EditedMesh.Dirty = true;
                return true;
            }
            return false;
        }
        
       public override void Inspect()
        {
            var col = GlobalBrush.Color;
            var msk = GlobalBrush.mask;
            if ("Paint All".Click().nl())
            {
                foreach (var v in EditedMesh.meshPoints)
                    msk.SetValuesOn(ref v.shadowBake, col);
                EditedMesh.Dirty = true;
            }
            GlobalBrush.ColorSliders().nl();

            /*var mat = InspectedPainter.Material;

             if (mat)
            {
                ShadowVolumeTexture shadVT = null;

                foreach (var vt in VolumeTexture.all)
                    if (vt.GetType() == typeof(ShadowVolumeTexture) && vt.materials.Contains(mat)) shadVT = (ShadowVolumeTexture)vt;

                if (shadVT && "Auto RayCast Shadows".Click().nl()) {

                    foreach (var v in EditedMesh.meshPoints) {

                        var vpos = v.WorldPos + v.GetWorldNormal() * 0.001f;

                        for (int i = 0; i < 3; i++)
                        {
                            var pnt = shadVT.lights.GetLight(i);
                            if (pnt)
                                v.shadowBake[i] = pnt.transform.position.RayCast(vpos) ? 0.6f : 0;

                        }
                    }

                    EditedMesh.Dirty = true;

                    "Ray Cast Complete".showNotificationIn3D_Views();
                }
            }*/
        }

    }
}