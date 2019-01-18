using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InvisibleUIGraphic : Graphic {

    public override bool Raycast(Vector2 sp, Camera eventCamera) => true;

    protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();
    
}
