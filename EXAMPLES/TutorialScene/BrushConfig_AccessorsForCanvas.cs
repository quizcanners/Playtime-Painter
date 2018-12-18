using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playtime_Painter
{

    public class BrushConfig_AccessorsForCanvas : MonoBehaviour
    {
        BrushConfig Brush => PainterCamera.Data.brushConfig; 

        public float Size2D { get { return Brush.Brush2D_Radius;  } set { Brush.Brush2D_Radius = value; } }

        public float Size3D { get { return Brush.Brush3D_Radius; } set { Brush.Brush3D_Radius = value; } }

        public float alpha { get { return Brush.colorLinear.a; } set { Brush.colorLinear.a = value; } }

    }
}
