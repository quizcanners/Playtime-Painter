using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter
{
    public class VertexShadowTool : MeshToolBase {
        public override string NameForPEGIdisplay => "vertex Shadow";

        public override bool MouseEventPointedVertex() {
           
            if ((EditorInputManager.GetMouseButton(0))) {
                if (PointedUV.SameAsLastFrame)
                    return true;
                BrushConfig bcf = Cfg.brushConfig;
                bcf.colorLinear.ToV4(ref PointedVertex.shadowBake, bcf.mask);
                MeshMGMT.editedMesh.Dirty = true;
                return true;
            }
            return false;
        }

        public override bool MouseEventPointedTriangle()
        {
            if ((EditorInputManager.GetMouseButton(0)))
            {
                if (PointedTris.SameAsLastFrame)
                    return true;
                BrushConfig bcf = Cfg.brushConfig;
                foreach (var uv in PointedTris.vertexes)
                bcf.colorLinear.ToV4(ref uv.meshPoint.shadowBake, bcf.mask);
                MeshMGMT.editedMesh.Dirty = true;
                return true;
            }
            return false;
        }

        #if PEGI
        public override bool Inspect()
        {
            var col = GlobalBrush.Color;
            var msk = GlobalBrush.mask;
            if ("Paint All".Click().nl())
            {
                foreach (var v in EditedMesh.meshPoints)
                    msk.Transfer(ref v.shadowBake, col);
                EditedMesh.Dirty = true;
            }
            GlobalBrush.ColorSliders().nl();

            var mat = InspectedPainter.Material;

            if (mat)
            {
                ShadowVolumeTexture shadVT = null;

                foreach (var vt in VolumeTexture.all)
                    if (vt.GetType() == typeof(ShadowVolumeTexture) && vt.materials.Contains(mat)) shadVT = (ShadowVolumeTexture)vt;

                if (shadVT && "Auto Raycast Shadows".Click().nl()) {

                    foreach (var v in EditedMesh.meshPoints) {

                        var vpos = v.WorldPos + v.GetWorldNormal() * 0.001f;

                        for (int i = 0; i < 3; i++)
                        {
                            var pnt = shadVT.lights.GetLight(i);
                            if (pnt)
                                v.shadowBake[i] = pnt.transform.position.RaycastGotHit(vpos) ? 0.6f : 0;

                        }
                    }

                    EditedMesh.Dirty = true;

                    "Raycast Complete".showNotificationIn3D_Views();
                }
            }
            return false;
        }
#endif
    }
}