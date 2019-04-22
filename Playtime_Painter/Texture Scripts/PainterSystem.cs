using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter
{

    public delegate bool PainterBoolPlugin(PlaytimePainter p);

    public abstract class PainterSystemKeepUnrecognizedCfg : PainterSystemCfg, IKeepUnrecognizedCfg, IPEGI
    {
        public UnrecognizedTagsList UnrecognizedStd { get; } = new UnrecognizedTagsList();

        #if PEGI
        public virtual bool Inspect() => UnrecognizedStd.Nested_Inspect();
        #endif
    }
    
    public abstract class PainterSystemCfg : PainterSystem, ICfg  
    {
        public abstract CfgEncoder Encode();

        public virtual void Decode(string data) => data.DecodeTagsFor(this);

        public abstract bool Decode(string tg, string data);
    }

    public class PainterSystemMono : MonoBehaviour, IKeepUnrecognizedCfg, IPEGI
    {
        #region Encode & Decode
        private readonly UnrecognizedTagsList _uTags = new UnrecognizedTagsList();
        public UnrecognizedTagsList UnrecognizedStd => _uTags;

        public virtual void Decode(string data) => data.DecodeTagsFor(this);

        public virtual bool Decode(string tg, string data) => true;

        public virtual CfgEncoder Encode() => this.EncodeUnrecognized();
        #endregion

        protected static PainterDataAndConfig Cfg => PainterCamera.Data;
        protected static PainterDataAndConfig TexMgmtData => PainterCamera.Data;
        protected static PainterCamera TexMGMT => PainterCamera.Inst;
        protected static BrushConfig GlobalBrush => TexMgmtData.brushConfig;
        protected static PlaytimePainter InspectedPainter => PlaytimePainter.inspected;
        protected static MeshManager MeshMGMT => MeshManager.Inst;
        protected static bool ApplicationIsQuitting => PainterSystem.applicationIsQuitting;

#if PEGI
        public virtual bool Inspect() => _uTags.Inspect();  
#endif

    }

    
    public class PainterSystem {
        protected static bool InspectAdvanced => BrushConfig.showAdvanced;
        protected static PainterDataAndConfig TexMGMTdata => PainterCamera.Data;
        protected static PainterDataAndConfig Cfg => PainterCamera.Data;
        protected static PainterCamera TexMGMT => PainterCamera.Inst;
        protected static Transform RtBrush => TexMGMT.brushRenderer.transform;
        protected static Mesh BrushMesh { set { TexMGMT.brushRenderer.meshFilter.mesh = value; } }
        protected static BrushConfig InspectedBrush => BrushConfig._inspectedBrush;
        protected static BrushConfig GlobalBrush => TexMGMTdata.brushConfig;
        protected static PlaytimePainter InspectedPainter => PlaytimePainter.inspected; 
        protected static ImageMeta InspectedImageMeta { get { var ip = InspectedPainter; return ip ? ip.ImgMeta : null; } }
        protected static GridNavigator Grid => GridNavigator.Inst();
        protected static MeshManager MeshMGMT => MeshManager.Inst;
        protected static EditableMesh EditedMesh => MeshManager.editedMesh;
        protected static bool docsEnabled => !PainterDataAndConfig.hideDocumentation;
        public static bool applicationIsQuitting;

    }

}