using UnityEngine;
using UnityEngine.UI;
using QuizCanners.Inspect;
using QuizCanners.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter.UI
{

    public class InvisibleUIGraphic : Graphic, IPEGI
    {
        public override void SetMaterialDirty() { }
        public override void SetVerticesDirty() { }
        public override bool Raycast(Vector2 sp, Camera eventCamera) => true;
        protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();

        public void Inspect()
        {
            var ico = raycastTarget;
            if ("Raycast Target".toggleIcon(ref ico))
                raycastTarget = ico;
        }

    }

    public static class InvisibleUIGraphicExtensions
    {
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
#endif
    }


#if UNITY_EDITOR
        [CustomEditor(typeof(InvisibleUIGraphic))] internal class InvisibleUIGraphicDrawer : PEGI_Inspector_Override { }
#endif
}