using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class PixelPerfcetUVmeshData : Image, IPEGI {
    
    public float roundingCourners = 0.5f;
    public bool feedPositionData = true;

    bool IsOverlay => canvas.renderMode == RenderMode.ScreenSpaceOverlay || !canvas.worldCamera;
    
    const string PIXEL_PERFECT_MATERIAL_TAG = "PixelPerfectUI";

#if PEGI
    public bool Inspect()
    {
        pegi.toggleDefaultInspector();

        bool changed = false;

        if (material) {
            var tag = material.GetTag(PIXEL_PERFECT_MATERIAL_TAG, false);

            if (tag.IsNullOrEmpty())
                "{0} doesn't have {1} tag".F(material.shader.name, PIXEL_PERFECT_MATERIAL_TAG).writeWarning();
            else
            {

                if (!canvas)
                    "No Canvas".writeWarning();

                bool usesPosition = tag.SameAs("Position");

                if (canvas) {
                    if ((canvas.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord1) == 0)
                    {
                        "Material requires Canvas to pass Edges data trough Texcoord1 data channel".writeWarning();
                        if ("Fix Canvas Texcoord1".Click().nl())
                            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
                    }
                    
                    if (feedPositionData && ((canvas.additionalShaderChannels & AdditionalCanvasShaderChannels.Normal) == 0)) {
                        "Material requires Canvas to pass Position Data trough Normal channel".writeWarning();
                        if ("Fix Canvas ".Click().nl())
                            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Normal;
                    }
                }

                if (!usesPosition && feedPositionData)
                    "Shader doesn't have PixelPerfectUI = Position Tag. Position updates may not be needed".writeHint();

            }
        }

        "Corners".edit(90, ref roundingCourners, 0, 0.75f).nl(ref changed);

        "Position Data".toggleIcon(ref feedPositionData).nl(ref changed);

        var mat = material;
        if ("Material".edit(90, ref mat).nl(ref changed))
            material = mat;

        var rt = raycastTarget;
        if ("Raycast Target".toggleIcon(ref rt).nl(ref changed))
            raycastTarget = rt;

        var sp = sprite;
        if ("Sprite".edit(90, ref sp).nl(ref changed))
            sprite = sp;

        var col = color;
        if (pegi.edit(ref col).nl(ref changed))
            color = col;

        if (changed)
            SetVerticesDirty();

        return changed;
    }
#endif

    protected override void OnPopulateMesh(VertexHelper vh) {

        Vector2 corner1 = (Vector2.zero - rectTransform.pivot) * rectTransform.rect.size;
        Vector2 corner2 = (Vector2.one - rectTransform.pivot) * rectTransform.rect.size;
        
        vh.Clear();

        UIVertex vert = UIVertex.simpleVert;
        
    

        var pos = Vector2.zero;

        if (feedPositionData) {
            pos = RectTransformUtility.WorldToScreenPoint(IsOverlay ? null : canvas.worldCamera, rectTransform.position);
            pos.Scale(new Vector2(1f / Screen.width, 1f / Screen.height));
        }
        
        Vector2 scale = rectTransform.rect.size;
        scale = new Vector2(Mathf.Max(0, (scale.x - scale.y) / scale.x), Mathf.Max(0, (scale.y - scale.x) / scale.y));

        float scaleToSided = scale.x - scale.y; // If x>0 - positive, else - negative

        vert.normal = new Vector4(pos.x, pos.y, scaleToSided, 0);
        vert.uv1 = new Vector2(scaleToSided, roundingCourners); // Replaced Edge smoothness with Scale
        vert.color = color;

        vert.uv0 = new Vector2(0, 0);
        vert.position = new Vector2(corner1.x, corner1.y);
        vh.AddVert(vert);

        vert.uv0 = new Vector2(0, 1);
        vert.position = new Vector2(corner1.x, corner2.y);
        vh.AddVert(vert);

        vert.uv0 = new Vector2(1, 1);
        vert.position = new Vector2(corner2.x, corner2.y);
        vh.AddVert(vert);

        vert.uv0 = new Vector2(1, 0);
        vert.position = new Vector2(corner2.x, corner1.y);
        vh.AddVert(vert);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

    Vector3 previousPos = Vector3.zero;

    void Update() {
        if (feedPositionData && rectTransform.position != previousPos) {
            previousPos = rectTransform.position;
            SetAllDirty();
        }
    }
}
