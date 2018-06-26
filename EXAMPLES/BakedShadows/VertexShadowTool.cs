using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter
{
    public class VertexShadowTool : MeshToolBase {
        public override string ToString() { return "vertex Shadow"; }

        public override bool MouseEventPointedVertex() {
           
            if ((EditorInputManager.GetMouseButton(0))) {
                if (pointedUV.sameAsLastFrame)
                    return true;
                BrushConfig bcf = cfg.brushConfig;
                bcf.colorLinear.ToV4(ref pointedVertex.shadowBake, bcf.mask);
                meshMGMT.edMesh.Dirty = true;
                return true;
            }
            return false;
        }

        public override bool MouseEventPointedTriangle()
        {
            if ((EditorInputManager.GetMouseButton(0)))
            {
                if (pointedTris.sameAsLastFrame)
                    return true;
                BrushConfig bcf = cfg.brushConfig;
                foreach (var uv in pointedTris.vertexes)
                bcf.colorLinear.ToV4(ref uv.meshPoint.shadowBake, bcf.mask);
                meshMGMT.edMesh.Dirty = true;
                return true;
            }
            return false;
        }

        #if PEGI
        public override bool PEGI()
        {
            var col = globalBrush.colorLinear.ToGamma();
            var msk = globalBrush.mask;
            if ("Paint All".Click().nl())
            {
                foreach (var v in editedMesh.vertices)
                    msk.Transfer(ref v.shadowBake, col);
                editedMesh.Dirty = true;
            }
            globalBrush.ColorSliders_PEGI().nl();

            var mat = inspectedPainter.material;

            if (mat != null)  {
                ShadowVolumeTexture shadVT = null;

                foreach (var vt in VolumeTexture.all)
                    if (vt.GetType() == typeof(ShadowVolumeTexture) && vt.materials.Contains(mat)) shadVT = (ShadowVolumeTexture)vt;

                        if (shadVT != null && "Auto Raycast Shadows".Click().nl()) {

                            foreach (var v in editedMesh.vertices) {

                                var vpos = v.worldPos + v.GetWorldNormal() * 0.001f;

                                for (int i = 0; i < 3; i++) {
                                    var pnt = shadVT.lights.GetLight(i);
                                    if (pnt != null)
                                    {

                                //Vector3 ray = pnt.transform.position - vpos;
                                v.shadowBake[i] = pnt.transform.position.RaycastGotHit(vpos) ? 0.6f : 0;
                                
                                //Physics.Raycast(new Ray(vpos, ray), ray.magnitude) ? 1 : 0;
                                    }
                                }
                            }

                            editedMesh.Dirty = true;

                            "Raycast Complete".showNotification();
                        }
            }
            return false;
        }
#endif
    }
}