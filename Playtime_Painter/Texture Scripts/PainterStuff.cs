using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter
{

    public delegate bool PainterBoolPlugin(PlaytimePainter p);

    public abstract class PainterStuffKeepUnrecognized_STD : PainterStuff_STD, IKeepUnrecognizedSTD, IPEGI
    {
        UnrecognizedTags_List uTags = new UnrecognizedTags_List();
        public UnrecognizedTags_List UnrecognizedSTD => uTags;

        #if PEGI
        public virtual bool Inspect() => uTags.Nested_Inspect();
        #endif
    }
    
    public abstract class PainterStuff_STD : PainterStuff, ISTD  
    {
        public abstract StdEncoder Encode();

        public virtual void Decode(string data) => data.DecodeTagsFor(this);

        public abstract bool Decode(string tg, string data);
    }

    /*
    public class PainterStuffScriptable : ScriptableObject
    {
        protected static PainterDataAndConfig Cfg { get { return PainterCamera.Data; } }
        protected static PainterDataAndConfig TexMGMTdata { get { return PainterCamera.Data; } }
        protected static PainterCamera TexMGMT { get { return PainterCamera.Inst; } }
        protected static Transform Rtbrush { get { return TexMGMT.brushRendy.transform; } }
        protected static Mesh BrushMesh { set { TexMGMT.brushRendy.meshFilter.mesh = value; } }
        protected static BrushConfig InspectedBrush { get { return BrushConfig._inspectedBrush; } }
        protected static BrushConfig GlobalBrush { get { return TexMGMTdata.brushConfig; } }
        protected static PlaytimePainter InspectedPainter { get { return PlaytimePainter.inspectedPainter; } }
        protected static ImageData InspectedImageData { get { var ip = InspectedPainter; return ip ? ip.ImgData : null; } }
        protected static GridNavigator Grid { get { return GridNavigator.Inst(); } }
        protected static MeshManager MeshMGMT { get { return MeshManager.Inst; } }
        protected static EditableMesh EditedMesh { get { return MeshManager.Inst.edMesh; } }
        protected static bool ApplicationIsQuitting { get { return PainterStuff.applicationIsQuitting; }  }
        protected static bool IsNowPlaytimeAndDisabled { get { return PainterStuff.IsNowPlaytimeAndDisabled; } }
    }
    */

    public class PainterStuffMono : MonoBehaviour, IKeepUnrecognizedSTD, IPEGI
    {

        protected UnrecognizedTags_List uTags = new UnrecognizedTags_List();
        public UnrecognizedTags_List UnrecognizedSTD => uTags;

        public virtual void Decode(string data) => data.DecodeTagsFor(this);

        public virtual bool Decode(string tg, string data) => true;

        public virtual StdEncoder Encode() => this.EncodeUnrecognized();
        protected static PainterDataAndConfig Cfg { get { return PainterCamera.Data; } }
        protected static PainterDataAndConfig TexMGMTdata { get { return PainterCamera.Data; } }
        protected static PainterCamera TexMGMT { get { return PainterCamera.Inst; } }
        protected static Transform Rtbrush { get { return TexMGMT.brushRenderer.transform; } }
        protected static Mesh BrushMesh { set { TexMGMT.brushRenderer.meshFilter.mesh = value; } }
        protected static BrushConfig InspectedBrush { get { return BrushConfig._inspectedBrush; } }
        protected static BrushConfig GlobalBrush { get { return TexMGMTdata.brushConfig; } }
        protected static PlaytimePainter InspectedPainter { get { return PlaytimePainter.inspectedPainter; } }
        protected static ImageMeta InspectedImageMeta { get { var ip = InspectedPainter; return ip?.ImgMeta; } }
        protected static GridNavigator Grid { get { return GridNavigator.Inst(); } }
        protected static MeshManager MeshMGMT { get { return MeshManager.Inst; } }
        protected static EditableMesh EditedMesh { get { return MeshManager.Inst?.editedMesh; } }
        protected static bool ApplicationIsQuitting { get { return PainterStuff.applicationIsQuitting; }  }
        protected static bool IsNowPlaytimeAndDisabled { get { return PainterStuff.IsNowPlaytimeAndDisabled; } }

#if PEGI
        public virtual bool Inspect() => uTags.Inspect();  
#endif

    }

    
    public class PainterStuff {
        protected static PainterDataAndConfig TexMGMTdata { get { return PainterCamera.Data; } }
        protected static PainterDataAndConfig Cfg { get { return PainterCamera.Data; } }
        protected static PainterCamera TexMGMT { get { return PainterCamera.Inst; } }
        protected static Transform Rtbrush { get { return TexMGMT.brushRenderer.transform; } }
        protected static Mesh BrushMesh { set { TexMGMT.brushRenderer.meshFilter.mesh = value; } }
        protected static BrushConfig InspectedBrush { get { return BrushConfig._inspectedBrush; } }
        protected static BrushConfig GlobalBrush { get { return TexMGMTdata.brushConfig; }  }
        protected static PlaytimePainter InspectedPainter { get { return PlaytimePainter.inspectedPainter; } }
        protected static ImageMeta InspectedImageMeta { get { var ip = InspectedPainter; return ip ? ip.ImgMeta : null; } }
        protected static GridNavigator Grid { get { return GridNavigator.Inst(); } }
        protected static MeshManager MeshMGMT { get { return MeshManager.Inst; } }
        protected static EditableMesh EditedMesh { get { return MeshManager.Inst.editedMesh; } }
        public static bool applicationIsQuitting;

        public static bool IsNowPlaytimeAndDisabled { get
            {
#if !BUILD_WITH_PAINTER
                if (Application.isPlaying)
                    return true;
#endif
                return false;
            }
        }

    }

}