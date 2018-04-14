using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace Playtime_Painter
{
    public class VertexShadowTool : MeshToolBase {
        public override string ToString() { return "vertex Shadow"; }

        public override void MouseEventPointedVertex() {
           
            if ((EditorInputManager.GetMouseButton(0))) {
                BrushConfig bcf = cfg.brushConfig;
                bcf.colorLinear.ToV4(ref vertex.shadowBake, bcf.mask);
                meshMGMT.edMesh.dirty = true;
            }
        }

        public override void MouseEventPointedTriangle()
        {
            if ((EditorInputManager.GetMouseButton(0)))
            {
                BrushConfig bcf = cfg.brushConfig;
                foreach (var uv in triangle.uvpnts)
                bcf.colorLinear.ToV4(ref uv.vert.shadowBake, bcf.mask);
                meshMGMT.edMesh.dirty = true;
            }
        }

        public override bool PEGI()
        {
            var col = globalBrush.colorLinear.ToGamma();
            var msk = globalBrush.mask;
            if ("Paint All".Click().nl())
            {
                foreach (var v in mesh.vertices)
                    msk.Transfer(ref v.shadowBake, col);
                mesh.dirty = true;
            }
            globalBrush.ColorSliders_PEGI().nl();

            var mat = inspectedPainter.material;

            if (mat != null)  {
                ShadowVolumeTexture shadVT = null;

                foreach (var vt in VolumeTexture.all)
                    if (vt.GetType() == typeof(ShadowVolumeTexture) && vt.materials.Contains(mat)) shadVT = (ShadowVolumeTexture)vt;

                        if (shadVT != null && "Auto Raycast Shadows".Click().nl()) {

                            foreach (var v in mesh.vertices) {

                                var vpos = v.worldPos + v.GetWorldNormal() * 0.001f;

                                for (int i = 0; i < 3; i++) {
                                    var pnt = shadVT.lights.GetLight(i);
                                    if (pnt != null)
                                    {
                                        Vector3 ray = pnt.transform.position - vpos;
                                        v.shadowBake[i] = Physics.Raycast(new Ray(vpos, ray), ray.magnitude) ? 0 : 1;
                                    }
                                }
                            }

                            mesh.dirty = true;

                            "Raycast Complete".showNotification();
                        }
            }
            return false;
        }

    }
}