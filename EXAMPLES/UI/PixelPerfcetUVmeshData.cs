using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class PixelPerfcetUVmeshData : Image, IPEGI {
    
    public float edgeSmoothness = 0.5f;
    public float roundingCourners = 0.5f;
    public bool feedPositionData = true;
    public Camera _camera;
    [NonSerialized]public Canvas _canvas;


    const string PIXEL_PERFECT_MATERIAL_TAG = "PixelPerfectUI";

    public bool Inspect()
    {
        pegi.toggleDefaultInspector();


        if (material) {
            var tag = material.GetTag(PIXEL_PERFECT_MATERIAL_TAG, false);

            if (tag.IsNullOrEmpty())
                "{0} doesn't have {1} tag".F(material.shader.name, PIXEL_PERFECT_MATERIAL_TAG).writeWarning();
            else
            {
                if (!_canvas && "Check Canvas".Click()) {
                    _canvas = GetComponentInParent<Canvas>();
                    if (!_canvas)
                        "No Canvas Found".showNotificationIn3D_Views();
                }

                bool needPosition = tag.SameAs("Position");

                if (_canvas) {
                    if ((_canvas.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord1) == 0)
                    {
                        "Material requires Canvas to pass Edges data trough Texcoord1 data channel".writeWarning();
                        if ("Fix Canvas Texcoord1".Click().nl())
                            _canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
                    }
                    
                    if (needPosition && ((_canvas.additionalShaderChannels & AdditionalCanvasShaderChannels.Tangent) == 0))
                    {
                        "Material requires Canvas to pass Position Data trough Tangent channel".writeWarning();
                        if ("Fix Canvas ".Click().nl())
                            _canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Tangent;
                    }
                }

                if (needPosition && !feedPositionData)
                    "Feed Position Data should be true".writeWarning();

            }
        }

       var changed = "Smoothness".edit(90, ref edgeSmoothness, 0, 1).nl();

        "Courners".edit(90, ref roundingCourners, 0, 0.75f).nl(ref changed);

        "Position Data".toggleIcon(ref feedPositionData).nl(ref changed);

        var mat = material;
        if ("Material".edit(90, ref mat).nl(ref changed))
            material = mat;
        
        var sp = sprite;
        if ("Sprite".edit(90, ref sp).nl(ref changed))
            sprite = sp;

        var col = color;
        if (pegi.edit(ref col).nl(ref changed))
            color = col;

        "Camera (Optional)".edit(110, ref _camera).nl(ref changed);

        if (changed)
            SetVerticesDirty();

        return changed;
    }

    protected override void OnPopulateMesh(VertexHelper vh) {

        Vector2 corner1 = (Vector2.zero - rectTransform.pivot) * rectTransform.rect.size;
        Vector2 corner2 = (Vector2.one - rectTransform.pivot) * rectTransform.rect.size;
        
        vh.Clear();

        UIVertex vert = UIVertex.simpleVert;
        
        vert.color = color;
        vert.uv1 = new Vector2(edgeSmoothness, roundingCourners); //pos;
        
        if (feedPositionData) {
            var pos = RectTransformUtility.WorldToScreenPoint(Camera.main, rectTransform.position);
            pos.Scale(new Vector2(1f / Screen.width, 1f / Screen.height));

            Vector2 scale = rectTransform.rect.size;
            scale = new Vector2(Mathf.Max(0, (scale.x - scale.y) / scale.x), Mathf.Max(0, (scale.y - scale.x) / scale.y));

            float scaleToSided = scale.x - scale.y; // If x>0 - positive, else - negative

            vert.normal = new Vector4(pos.x, pos.y, scaleToSided, 0);
        }
        
        vert.uv0 = new Vector2(0, 0);
        vert.uv2 = new Vector2(1, 1);
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
    
    protected override void OnEnable() {

        base.OnEnable();

        #if UNITY_EDITOR
        if (!_canvas)
            _canvas = GetComponentInParent<Canvas>();
        #endif
    }

}
