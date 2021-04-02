using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter.UI
{
    public static class ShaderTags
    {
        public static readonly ShaderTag PixelPerfectUi = new ShaderTag("PixelPerfectUI");

        public static class PixelPerfectUis
        {
            public static readonly ShaderTagValue Simple = new ShaderTagValue("Simple", PixelPerfectUi);
            public static readonly ShaderTagValue Position = new ShaderTagValue("Position", PixelPerfectUi);
            public static readonly ShaderTagValue AtlasedPosition = new ShaderTagValue("AtlasedPosition", PixelPerfectUi);
            public static readonly ShaderTagValue FadePosition = new ShaderTagValue("FadePosition", PixelPerfectUi);
        }

        public static readonly ShaderTag SpriteRole = new ShaderTag("SpriteRole");

        public static class SpriteRoles
        {
            public static readonly ShaderTagValue Hide = new ShaderTagValue("Hide", SpriteRole);
            public static readonly ShaderTagValue Tile = new ShaderTagValue("Tile", SpriteRole);
            public static readonly ShaderTagValue Normal = new ShaderTagValue("Normal", SpriteRole);
        }

        public static readonly ShaderTag PerEdgeData = new ShaderTag("PerEdgeData");

        public static class PerEdgeRoles
        {
            public static readonly ShaderTagValue UnlinkedCourners = new ShaderTagValue("Unlinked", PerEdgeData);
            public static readonly ShaderTagValue LinkedCourners = new ShaderTagValue("Linked", PerEdgeData);
        }

    }

    public static class RoundedUiExtensions
    {

        public static void AddFull(this VertexHelper vh, UIVertex vert) =>
#if UNITY_2019_1_OR_NEWER
         vh.AddVert(vert.position, vert.color, vert.uv0, vert.uv1, vert.uv2, vert.uv3, vert.normal, vert.tangent);
#else
         vh.AddVert(vert.position, vert.color, vert.uv0, vert.uv1, vert.normal, vert.tangent);
#endif

#if UNITY_EDITOR

        [MenuItem("GameObject/UI/Playtime Painter/Invisible Raycat Target", false, 0)]
        private static void CreateInvisibleRaycastTarget()
        {
            var els = QcUnity.CreateUiElement<InvisibleUIGraphic>(Selection.gameObjects);

            foreach (var el in els)
            {
                el.name = "[]";
            }

        }

        [MenuItem("GameObject/UI/Playtime Painter/Rounded UI Graphic", false, 0)]
        private static void CreateRoundedUiElement()
        {
            QcUnity.CreateUiElement<RoundedGraphic>(Selection.gameObjects, onCreate: el =>
            {
                el.maskable = el.GetComponentInParent<Mask>();
                el.raycastTarget = false; 
            });
            /* bool createdForSelection = false;

             if (Selection.gameObjects.Length > 0)
             {

                 foreach (var go in Selection.gameObjects)
                 {
                     if (go.GetComponentInParent<Canvas>())
                     {
                         CreateRoundedUiElement(go);
                         createdForSelection = true;
                     }
                 }
             }

             if (!createdForSelection)
             {

                 var canvas = Object.FindObjectOfType<Canvas>();

                 if (!canvas)
                     canvas = new GameObject("Canvas").AddComponent<Canvas>();

                 CreateRoundedUiElement(canvas.gameObject);

             }
             */
        }

#endif

        public static UIVertex Set(this UIVertex vertex, float uvX, float uvY, Vector2 posX, Vector2 posY)
        {
            vertex.uv0 = new Vector2(uvX, uvY);
            vertex.position = new Vector2(posX.x, posY.y);
            return vertex;
        }
    }

    #region Inspector override
#if UNITY_EDITOR
    [CustomEditor(typeof(RoundedGraphic))]
    public class PixelPerfectShaderDrawer : PEGI_Inspector_Mono<RoundedGraphic> { }
#endif
    #endregion

    public class PixelPerfectMaterialDrawer : PEGI_Inspector_Material
    {
        private static readonly ShaderProperty.FloatValue Softness = new ShaderProperty.FloatValue(RoundedGraphic.EDGE_SOFTNESS_FLOAT);

        private static readonly ShaderProperty.TextureValue Outline = new ShaderProperty.TextureValue("_OutlineGradient");

        public override bool Inspect(Material mat)
        {

            var changed = pegi.toggleDefaultInspector(mat);

            mat.edit(Softness, "Softness", 0, 1).nl(ref changed);

            mat.edit(Outline).nl(ref changed);

            if (mat.IsKeywordEnabled(RoundedGraphic.UNLINKED_VERTICES))
                "UNLINKED VERTICES".nl();

            var go = QcUnity.GetFocusedGameObject();

            if (go)
            {

                var rndd = go.GetComponent<RoundedGraphic>();

                if (!rndd)
                    "No RoundedGrahic.cs detected, shader needs custom data.".writeWarning();
                else if (!rndd.enabled)
                    "Controller is disabled".writeWarning();

            }

            return changed;
        }
    }

}

