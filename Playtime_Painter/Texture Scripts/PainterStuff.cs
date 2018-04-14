using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StoryTriggerData;
using System;
using PlayerAndEditorGUI;

namespace Playtime_Painter
{

    [Serializable]
    public abstract class PainterStuff_STD : PainterStuff, iSTD {
        public abstract stdEncoder Encode();
        public virtual iSTD Reboot(string data)
        {
            new stdDecoder(data).DecodeTagsFor(this);
            return this;
        }
        public virtual bool PEGI() { pegi.nl(); (GetType() + " class has no PEGI() function.").nl(); return false; }
        public abstract void Decode(string tag, string data);
        public abstract string getDefaultTagName();

    }


    public class PainterStuffScriptable : ScriptableObject
    {

        protected static PainterManager texMGMT { get { return PainterManager.inst; } }
        protected static Transform rtbrush { get { return texMGMT.brushRendy.transform; } }
        protected static Mesh brushMesh { set { texMGMT.brushRendy.meshFilter.mesh = value; } }
        protected static PainterConfig cfg { get { return PainterConfig.inst; } }
        protected static BrushConfig inspectedBrush { get { return BrushConfig._inspectedBrush; } }
        protected static BrushConfig globalBrush { get { return cfg.brushConfig; } }
        protected static PlaytimePainter inspectedPainter { get { return PlaytimePainter.inspectedPainter; } }
        protected static ImageData inspectedImageData { get { var ip = inspectedPainter; return ip != null ? ip.imgData : null; } }
        protected static GridNavigator grid { get { return GridNavigator.inst(); } }
        protected static MeshManager meshMGMT { get { return MeshManager.inst; } }
        protected static EditableMesh mesh { get { return MeshManager.inst.edMesh; } }
    }

    public class PainterStuffMono : MonoBehaviour
    {

        protected static PainterManager texMGMT { get { return PainterManager.inst; } }
        protected static Transform rtbrush { get { return texMGMT.brushRendy.transform; } }
        protected static Mesh brushMesh { set { texMGMT.brushRendy.meshFilter.mesh = value; } }
        protected static PainterConfig cfg { get { return PainterConfig.inst; } }
        protected static BrushConfig inspectedBrush { get { return BrushConfig._inspectedBrush; } }
        protected static BrushConfig globalBrush { get { return cfg.brushConfig; } }
        protected static PlaytimePainter inspectedPainter { get { return PlaytimePainter.inspectedPainter; } }
        protected static ImageData inspectedImageData { get { var ip = inspectedPainter; return ip != null ? ip.imgData : null; } }
        protected static GridNavigator grid { get { return GridNavigator.inst(); } }
        protected static MeshManager meshMGMT { get { return MeshManager.inst; } }
        protected static EditableMesh mesh { get { return MeshManager.inst.edMesh; } }
    }

    [Serializable]
    public class PainterStuff {

        protected static PainterManager texMGMT { get { return PainterManager.inst; } }
        protected static Transform rtbrush { get { return texMGMT.brushRendy.transform; } }
        protected static Mesh brushMesh { set { texMGMT.brushRendy.meshFilter.mesh = value; } }
        protected static PainterConfig cfg { get { return PainterConfig.inst; } }
        protected static BrushConfig inspectedBrush { get { return BrushConfig._inspectedBrush; } }
        protected static BrushConfig globalBrush { get { return cfg.brushConfig; }  }
        protected static PlaytimePainter inspectedPainter { get { return PlaytimePainter.inspectedPainter; } }
        protected static ImageData inspectedImageData { get { var ip = inspectedPainter; return ip != null ? ip.imgData : null; } }
        protected static GridNavigator grid { get { return GridNavigator.inst(); } }
        protected static MeshManager meshMGMT { get { return MeshManager.inst; } }
        protected static EditableMesh mesh { get { return MeshManager.inst.edMesh; } }
    }


}