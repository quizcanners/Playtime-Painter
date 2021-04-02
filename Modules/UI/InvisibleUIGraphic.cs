using UnityEngine;
using UnityEngine.UI;
using QuizCanners.Inspect;
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
    
#if UNITY_EDITOR
    [CustomEditor(typeof(InvisibleUIGraphic))]
    public class InvisibleUIGraphicDrawer : PEGI_Inspector_Mono<InvisibleUIGraphic> { }
#endif
}