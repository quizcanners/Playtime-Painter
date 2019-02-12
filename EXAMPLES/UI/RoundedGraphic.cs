using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.UI;

namespace Playtime_Painter.Examples
{

    [ExecuteInEditMode]
    public class RoundedGraphic : Image, IPEGI
    {

        static List<Shader> compatibleShaders;

        List<Shader> CompatibleShaders { get {
                if (compatibleShaders == null) {
                    compatibleShaders = new List<Shader>()
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/BumpedButton"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Box"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/PixelPerfect"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Outline"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/ButtonWithShadow"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Shadow"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Gradient"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/PixelLine"));
                }
                return compatibleShaders;
            } }

        [SerializeField] float[] roundedCourners = new float[1];
        
        public float GetCourner(int index) => roundedCourners[index % roundedCourners.Length];

        public void SetCourner(int index, float value) => roundedCourners[index % roundedCourners.Length] = value;

        public bool LinkedCourners
        {
            get { return roundedCourners.Length == 1; }

            set
            {
                int targetValue = value ? 1 : 4;

                if (targetValue != roundedCourners.Length) {

                    if (material)
                        material.SetShaderKeyword(UNLINKED_VERTICES, targetValue>1);



                    var tmp = roundedCourners[0];

                    roundedCourners = new float[targetValue];

                    for (int i = 0; i < roundedCourners.Length; i++)
                        roundedCourners[i] = tmp;
                }
            }
        }
        
        public bool feedPositionData = true;

        bool IsOverlay => canvas ? (canvas.renderMode == RenderMode.ScreenSpaceOverlay || !canvas.worldCamera) : false;

        public const string PIXEL_PERFECT_MATERIAL_TAG = "PixelPerfectUI";
        public const string SPRITE_ROLE_MATERIAL_TAG = "SpriteRole";
        public const string UNLINKED_VERTICES = "_UNLINKED";
        public const string EDGE_SOFTNESS_FLOAT = "_Edges";

        #if PEGI
        public bool Inspect()
        {
            pegi.toggleDefaultInspector();

            bool changed = false;

            if (material)
            {
                var tag = material.GetTag(PIXEL_PERFECT_MATERIAL_TAG, false);

                if (tag.IsNullOrEmpty())
                    "{0} doesn't have {1} tag".F(material.shader.name, PIXEL_PERFECT_MATERIAL_TAG).writeWarning();
                else
                {

                    if (!canvas)
                        "No Canvas".writeWarning();

                    bool usesPosition = tag.SameAs("Position");

                    if (canvas)
                    {
                        if ((canvas.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord1) == 0)
                        {
                            "Material requires Canvas to pass Edges data trough Texcoord1 data channel".writeWarning();
                            if ("Fix Canvas Texcoord1".Click().nl())
                                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
                        }

                        if (feedPositionData && ((canvas.additionalShaderChannels & AdditionalCanvasShaderChannels.Normal) == 0))
                        {
                            "Material requires Canvas to pass Position Data trough Normal channel".writeWarning();
                            if ("Fix Canvas ".Click().nl())
                                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Normal;
                        }
                    }

                    if (!usesPosition && feedPositionData)
                        "Shader doesn't have PixelPerfectUI = Position Tag. Position updates may not be needed".writeHint();

                }
            }

            bool linked = LinkedCourners;

            "Material Is Unlinked: {0}".F(material.IsKeywordEnabled(UNLINKED_VERTICES)).nl();

            if (material && (linked == material.IsKeywordEnabled(UNLINKED_VERTICES)))
                material.SetShaderKeyword(UNLINKED_VERTICES, !linked);
                
            if (pegi.toggle(ref linked, icon.Link, icon.UnLinked).changes(ref changed))
                LinkedCourners = linked;

            for (int i = 0; i < roundedCourners.Length; i++) {
                var crn = roundedCourners[i];

                if ("Corner{0}".F(linked ? "s" : (" " + i.ToString())).edit(90, ref crn, 0, 1f).nl(ref changed)) 
                    roundedCourners[i] = crn;
            }

            pegi.nl();
            var mat = material;

            if (material) {
                
                if (!Application.isPlaying)
                {
                    var path = material.GetAssetFolder();
                    if (path.IsNullOrEmpty())
                        "Material is not saved as asset. Click COPY next to it to save as asset".writeHint();
                }

                var n = material.name;
                if ("Material".editDelayed(40, ref n))
                    material.RenameAsset(n);
            }

            if (pegi.edit(ref mat, 60).changes(ref changed) || pegi.ClickDuplicate(ref mat).nl(ref changed))
                material = mat;
            
            if (material) {
                var shader = material.shader;

                if ("Shaders".select(60, ref shader, CompatibleShaders, false, true).nl(ref changed))
                    material.shader = shader;
            }

            "Position Data".toggleIcon(ref feedPositionData).nl(ref changed);
            var rt = raycastTarget;
            if ("Clickable".toggleIcon("Is Raycast Target",ref rt).nl(ref changed))
                raycastTarget = rt;
            
            string spriteTag = material ? material.GetTag(SPRITE_ROLE_MATERIAL_TAG, false) : null;

            bool noTag = spriteTag.IsNullOrEmpty();

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

            Vector2 corner1 = (Vector2.zero - rectTransform.pivot) * rectTransform.rect.size;
            Vector2 corner2 = (Vector2.one - rectTransform.pivot) * rectTransform.rect.size;

            vh.Clear();

            UIVertex vert = UIVertex.simpleVert;

            var pos = Vector2.zero;

            if (feedPositionData) {
                pos = RectTransformUtility.WorldToScreenPoint(IsOverlay ? null : (canvas ? canvas.worldCamera : null) , rectTransform.position);
                pos.Scale(new Vector2(1f / Screen.width, 1f / Screen.height));
            }

            Vector2 scale = rectTransform.rect.size;
            scale = new Vector2(Mathf.Max(0, (scale.x - scale.y) / scale.x), Mathf.Max(0, (scale.y - scale.x) / scale.y));

            float scaleToSided = scale.x - scale.y; // If x>0 - positive, else - negative

            vert.normal = new Vector4(pos.x, pos.y, scaleToSided, 0);
            vert.uv1 = new Vector2(scaleToSided, GetCourner(0));  // Replaced Edge smoothness with Scale
            vert.color = color;

            vert.uv0 = new Vector2(0, 0);
            vert.position = new Vector2(corner1.x, corner1.y);
            vh.AddVert(vert);

            vert.uv0 = new Vector2(0, 1);
            vert.uv1.y =  GetCourner(1);
            vert.position = new Vector2(corner1.x, corner2.y);
            vh.AddVert(vert);

            vert.uv0 = new Vector2(1, 1);
            vert.uv1.y = GetCourner(2);
            vert.position = new Vector2(corner2.x, corner2.y);
            vh.AddVert(vert);

            vert.uv0 = new Vector2(1, 0);
            vert.uv1.y = GetCourner(3);
            vert.position = new Vector2(corner2.x, corner1.y);
            vh.AddVert(vert);

            if (LinkedCourners) {
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

                
                vert.uv1.y = GetCourner(0);
                vh.AddVert(vert.Set(0, 0.5f, corner1, cornMid)); //4
                vh.AddVert(vert.Set(0.5f, 0, cornMid, corner1)); //5
               
                vert.uv1.y = GetCourner(1);
                vh.AddVert(vert.Set(0.5f, 1, cornMid, corner2)); //6
                vh.AddVert(vert.Set(0, 0.5f, corner1, cornMid)); //7

                vert.uv1.y = GetCourner(2);
                vh.AddVert(vert.Set(1, 0.5f, corner2, cornMid)); //8
                vh.AddVert(vert.Set(0.5f, 1, cornMid, corner2)); //9

                vert.uv1.y = GetCourner(3);
                vh.AddVert(vert.Set(0.5f, 0, cornMid, corner1)); //10
                vh.AddVert(vert.Set(1, 0.5f, corner2, cornMid)); //11



                vert.uv1.y = GetCourner(0);
                vh.AddVert(vert.Set(0.5f, 0.5f, cornMid, cornMid)); //12

                vert.uv1.y = GetCourner(1);
                vh.AddVert(vert.Set(0.5f, 0.5f, cornMid, cornMid)); //13

                vert.uv1.y = GetCourner(2);
                vh.AddVert(vert.Set(0.5f, 0.5f, cornMid, cornMid)); //14

                vert.uv1.y = GetCourner(3);
                vh.AddVert(vert.Set(0.5f, 0.5f, cornMid, cornMid)); //15



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

        Vector3 previousPos = Vector3.zero;

        void Update()
        {
            if (feedPositionData && rectTransform.position != previousPos)
            {
                previousPos = rectTransform.position;
                SetAllDirty();
            }
        }
    }
    
    public static class RoundedUiExtensions
    {

        #if UNITY_EDITOR
        [MenuItem("GameObject/UI/Playtime Painter/Rounded UI Graphic", false, 0)]
        static void CreateRoundedUIElement() {

            var rg = new GameObject("Rounded UI Element").AddComponent<RoundedGraphic>();

            var go = rg.gameObject;

            var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();

            if (!canvas)
                canvas = new GameObject("Canvas").AddComponent<Canvas>();

            GameObjectUtility.SetParentAndAlign(go, canvas.gameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;

            rg.material = UnityEngine.Object.Instantiate(rg.material);
        }
        #endif

        public static UIVertex Set(this UIVertex vert, float uvX, float uvY, Vector2 posX, Vector2 posY) {
            vert.uv0 = new Vector2(uvX, uvY);
            vert.position = new Vector2(posX.x, posY.y);
            return vert;
        }


    }


    #if UNITY_EDITOR
    [CustomEditor(typeof(RoundedGraphic))]
    public class PixelPerfcetUVmeshDataDrawer : PEGI_Inspector<RoundedGraphic> { }
    #endif

}