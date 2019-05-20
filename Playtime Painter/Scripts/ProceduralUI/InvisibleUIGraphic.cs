using UnityEngine;
using UnityEngine.UI;

namespace PlaytimePainter
{

    public class InvisibleUIGraphic : Graphic
    {
        public override bool Raycast(Vector2 sp, Camera eventCamera) => true;
        protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();
    }
}