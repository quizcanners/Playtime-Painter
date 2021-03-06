﻿using UnityEngine;
using UnityEngine.UI;
using QuizCanners.Inspect;
using QuizCanners.Utils;

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
        [UnityEditor.MenuItem("GameObject/UI/Playtime Painter/Invisible Raycat Target", false, 0)]
        private static void CreateInvisibleRaycastTarget()
        {
            var els = QcUnity.CreateUiElement<InvisibleUIGraphic>(UnityEditor.Selection.gameObjects);

            foreach (var el in els)
            {
                el.name = "[]";
            }

        }
#endif
    }



        [PEGI_Inspector_Override(typeof(InvisibleUIGraphic))] internal class InvisibleUIGraphicDrawer : PEGI_Inspector_Override { }

}