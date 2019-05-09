using UnityEngine;
using UnityEngine.UI;

namespace Playtime_Painter.Examples
{

    public class InvisibleUIGraphic : Graphic
    {
        public override bool Raycast(Vector2 sp, Camera eventCamera) => true;
        protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();
    }
}