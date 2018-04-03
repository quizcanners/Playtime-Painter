using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playtime_Painter
{

    public class PainterStuff
    {

        protected static PainterManager mgmt { get { return PainterManager.inst; } }
        protected static Transform rtbrush { get { return mgmt.brushRendy.transform; } }
        protected static Mesh brushMesh { set { mgmt.brushRendy.meshFilter.mesh = value; } }
        protected static PainterConfig cfg { get { return PainterConfig.inst; } }
        protected static BrushConfig brush { get { return BrushConfig.inspectedBrush; } }
        protected static PlaytimePainter painter { get { return PlaytimePainter.inspectedPainter; } }
     
    }
}