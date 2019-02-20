using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.UI;

namespace Playtime_Painter.Examples {

    [ExecuteInEditMode]
    public class RoundedGraphic : Image, IPEGI {

        private static List<Shader> _compatibleShaders;

        public static List<Shader> CompatibleShaders { get {
                if (_compatibleShaders == null) {
                    _compatibleShaders = new List<Shader>()
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/BumpedButton"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Box"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/PixelPerfect"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Outline"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/ButtonWithShadow"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Shadow"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Gradient"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/PixelLine"));
                }
                return _compatibleShaders;
            } 
        }

        [SerializeField] private float[] _roundedCorners = new float[1];
        
        private float GetCorner(int index) => _roundedCorners[index % _roundedCorners.Length];

        private void SetCorner(int index, float value) => _roundedCorners[index % _roundedCorners.Length] = value;

        public bool LinkedCorners
        {
            get { return _roundedCorners.Length == 1; }

            set
            {
                var targetValue = value ? 1 : 4;

                if (targetValue == _roundedCorners.Length) return;

                if (material)
                    material.SetShaderKeyword(UNLINKED_VERTICES, targetValue>1);

                var tmp = _roundedCorners[0];

                _roundedCorners = new float[targetValue];

                for (var i = 0; i < _roundedCorners.Length; i++)
                    _roundedCorners[i] = tmp;
            }
        }
        
        public bool feedPositionData = true;

        private bool IsOverlay
        {
            get
            {
                var c = canvas;
                return c && (c.renderMode == RenderMode.ScreenSpaceOverlay || !c.worldCamera);
            }
        } 

        public const string PIXEL_PERFECT_MATERIAL_TAG = "PixelPerfectUI";
        public const string SPRITE_ROLE_MATERIAL_TAG = "SpriteRole"; // "Hide", "Tile"
        public const string UNLINKED_VERTICES = "_UNLINKED";
        public const string EDGE_SOFTNESS_FLOAT = "_Edges";

        #if PEGI
        public bool Inspect()
        {
            pegi.toggleDefaultInspector();

            var mat = material;

            var can = canvas;
            
            var shad = mat.shader;
            
            var changed = false;

            var usesPosition = false;

            if (mat)
            {
                var pixPfTag = mat.GetTag(PIXEL_PERFECT_MATERIAL_TAG, false);

                if (pixPfTag.IsNullOrEmpty())
                    "{0} doesn't have {1} tag".F(shad.name, PIXEL_PERFECT_MATERIAL_TAG).writeWarning();
                else
                {

                    usesPosition = pixPfTag.SameAs("Position");

                    if (!can)
                        "No Canvas".writeWarning();
                    else {
                        if ((can.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord1) == 0)
                        {
                            "Material requires Canvas to pass Edges data trough TexCoord1 data channel".writeWarning();
                            if ("Fix Canvas TexCoord1".Click().nl())
                                can.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
                        }

                        if (feedPositionData && ((can.additionalShaderChannels & AdditionalCanvasShaderChannels.Normal) == 0))
                        {
                            "Material requires Canvas to pass Position Data trough Normal channel".writeWarning();
                            if ("Fix Canvas ".Click().nl())
                                can.additionalShaderChannels |= AdditionalCanvasShaderChannels.Normal;
                        }
                    }
                }
            }

            var linked = LinkedCorners;

            "Material Is Unlinked: {0}".F(mat.IsKeywordEnabled(UNLINKED_VERTICES)).nl();

            if (mat && (linked == mat.IsKeywordEnabled(UNLINKED_VERTICES)))
                mat.SetShaderKeyword(UNLINKED_VERTICES, !linked);
                
            if (pegi.toggle(ref linked, icon.Link, icon.UnLinked).changes(ref changed))
                LinkedCorners = linked;

            for (var i = 0; i < _roundedCorners.Length; i++) {
                var crn = _roundedCorners[i];

                if ("Corner{0}".F(linked ? "s" : (" " + i.ToString())).edit(90, ref crn, 0, 1f).nl(ref changed)) 
                    _roundedCorners[i] = crn;
            }

            pegi.nl();
            
    

            if (mat) {
                
                if (!Application.isPlaying)
                {
                    var path = mat.GetAssetFolder();
                    if (path.IsNullOrEmpty())
                        "Material is not saved as asset. Click COPY next to it to save as asset".writeHint();
                }

                var n = mat.name;
                if ("Material".editDelayed(40, ref n))
                    mat.RenameAsset(n);
            }

            if (pegi.edit(ref mat, 60).changes(ref changed) || pegi.ClickDuplicate(ref mat).nl(ref changed))
                material = mat;
            
            if (mat) {
                

                if ("Shaders".select(60, ref shad, CompatibleShaders, false, true).changes(ref changed))
                    mat.shader = shad;

                if (icon.Refresh.Click("Refresh compatible shaders list"))
                    _compatibleShaders = null;
            }

            pegi.nl();

            if (usesPosition || feedPositionData)
                "Position Data".toggleIcon(ref feedPositionData).changes(ref changed);

            if (!usesPosition && feedPositionData)
            {
                "Shader doesn't have PixelPerfectUI = Position Tag. Position updates may not be needed".write();
                icon.Warning.write("Unnessessary data");
            }
            pegi.nl();

            var rt = raycastTarget;
            if ("Clickable".toggleIcon("Is RayCast Target",ref rt).nl(ref changed))
                raycastTarget = rt;
            
            var spriteTag = mat ? mat.GetTag(SPRITE_ROLE_MATERIAL_TAG, false) : null;

            var noTag = spriteTag.IsNullOrEmpty();

            if (noTag || !spriteTag.SameAs("Hide")) {

                if (noTag)
                    spriteTag = "Sprite";

                var sp = sprite;
                if (spriteTag.edit(90, ref sp).nl(ref changed))
                    sprite = sp;
            }

            var col = color;
            if (pegi.edit(ref col).nl(ref changed))
                color = col;

            if (changed)
                SetVerticesDirty();

            return changed;
        }
        #endif

        protected override void OnPopulateMesh(VertexHelper vh)
        {

            var rt = rectTransform;
            var piv = rt.pivot;
            var rectSize = rt.rect.size;
            var corner1 = (Vector2.zero - piv) * rectSize;
            var corner2 = (Vector2.one - piv) * rectSize;

            vh.Clear();

            var vertex = UIVertex.simpleVert;

            var pos = Vector2.zero;

            if (feedPositionData)
            {
                var myCanvas = canvas;
                pos = RectTransformUtility.WorldToScreenPoint(IsOverlay ? null : (myCanvas ? myCanvas.worldCamera : null) , rt.position);
                pos.Scale(new Vector2(1f / Screen.width, 1f / Screen.height));
            }

            rectSize = new Vector2(Mathf.Max(0, (rectSize.x - rectSize.y) / rectSize.x), Mathf.Max(0, (rectSize.y - rectSize.x) / rectSize.y));

            var scaleToSided = rectSize.x - rectSize.y; // If x>0 - positive, else - negative

            vertex.normal = new Vector4(pos.x, pos.y, scaleToSided, 0);
            vertex.uv1 = new Vector2(scaleToSided, GetCorner(0));  // Replaced Edge smoothness with Scale
            vertex.color = color;

            vertex.uv0 = new Vector2(0, 0);
            vertex.position = new Vector2(corner1.x, corner1.y);
            vh.AddVert(vertex);

            vertex.uv0 = new Vector2(0, 1);
            vertex.uv1.y =  GetCorner(1);
            vertex.position = new Vector2(corner1.x, corner2.y);
            vh.AddVert(vertex);

            vertex.uv0 = new Vector2(1, 1);
            vertex.uv1.y = GetCorner(2);
            vertex.position = new Vector2(corner2.x, corner2.y);
            vh.AddVert(vertex);

            vertex.uv0 = new Vector2(1, 0);
            vertex.uv1.y = GetCorner(3);
            vertex.position = new Vector2(corner2.x, corner1.y);
            vh.AddVert(vertex);

            if (LinkedCorners) {
                //1  2
                //0  3
                vh.AddTriangle(0, 1, 2);
                vh.AddTriangle(2, 3, 0);
            } else {
                //1    6,9    2
                //7  13  14   8
                //4  12  15   11
                //0    5,10   3
                
                var cornMid = (corner1 + corner2) * 0.5f;
                
                vertex.uv1.y = GetCorner(0);
                vh.AddVert(vertex.Set(0, 0.5f, corner1, cornMid)); //4
                vh.AddVert(vertex.Set(0.5f, 0, cornMid, corner1)); //5
               
                vertex.uv1.y = GetCorner(1);
                vh.AddVert(vertex.Set(0.5f, 1, cornMid, corner2)); //6
                vh.AddVert(vertex.Set(0, 0.5f, corner1, cornMid)); //7

                vertex.uv1.y = GetCorner(2);
                vh.AddVert(vertex.Set(1, 0.5f, corner2, cornMid)); //8
                vh.AddVert(vertex.Set(0.5f, 1, cornMid, corner2)); //9

                vertex.uv1.y = GetCorner(3);
                vh.AddVert(vertex.Set(0.5f, 0, cornMid, corner1)); //10
                vh.AddVert(vertex.Set(1, 0.5f, corner2, cornMid)); //11
                
                vertex.uv1.y = GetCorner(0);
                vh.AddVert(vertex.Set(0.5f, 0.5f, cornMid, cornMid)); //12

                vertex.uv1.y = GetCorner(1);
                vh.AddVert(vertex.Set(0.5f, 0.5f, cornMid, cornMid)); //13

                vertex.uv1.y = GetCorner(2);
                vh.AddVert(vertex.Set(0.5f, 0.5f, cornMid, cornMid)); //14

                vertex.uv1.y = GetCorner(3);
                vh.AddVert(vertex.Set(0.5f, 0.5f, cornMid, cornMid)); //15
                
                vh.AddTriangle(0, 4, 5);
                vh.AddTriangle(1, 6, 7);
                vh.AddTriangle(2, 8, 9);
                vh.AddTriangle(3, 10, 11);

                vh.AddTriangle(12, 5, 4);
                vh.AddTriangle(13, 7, 6);
                vh.AddTriangle(14, 9, 8);
                vh.AddTriangle(15, 11, 10);

            }
        }

        Vector3 _previousPos = Vector3.zero;

        private void Update()
        {
            if (!feedPositionData || rectTransform.position == _previousPos) return;
            
            _previousPos = rectTransform.position;
            
            SetAllDirty();
        }
    }
    
    public static class RoundedUiExtensions
    {

        #if UNITY_EDITOR
        [MenuItem("GameObject/UI/Playtime Painter/Rounded UI Graphic", false, 0)]
        private static void CreateRoundedUiElement() {

            var rg = new GameObject("Rounded UI Element").AddComponent<RoundedGraphic>();

            var go = rg.gameObject;

            var canvas = Object.FindObjectOfType<Canvas>();

            if (!canvas)
                canvas = new GameObject("Canvas").AddComponent<Canvas>();

            GameObjectUtility.SetParentAndAlign(go, canvas.gameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;

            rg.material = Object.Instantiate(rg.material);
        }
        #endif

        public static UIVertex Set(this UIVertex vertex, float uvX, float uvY, Vector2 posX, Vector2 posY) {
            vertex.uv0 = new Vector2(uvX, uvY);
            vertex.position = new Vector2(posX.x, posY.y);
            return vertex;
        }


    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(RoundedGraphic))]
    public class PixelPerfectShaderDrawer : PEGI_Inspector<RoundedGraphic> { }
    #endif

}