using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using PlaytimePainter.MeshEditing;
using static QuizCannersUtilities.ShaderProperty;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter
{

    public delegate bool PainterBoolPlugin(PlaytimePainter p);

    public abstract class PainterSystemKeepUnrecognizedCfg : PainterSystemCfg, IKeepUnrecognizedCfg, IPEGI
    {
        public UnrecognizedTagsList UnrecognizedStd { get; } = new UnrecognizedTagsList();
        
        public virtual bool Inspect() => UnrecognizedStd.Nested_Inspect();
      
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
        protected static MeshEditorManager MeshMGMT => MeshEditorManager.Inst;
        protected static bool ApplicationIsQuitting => PainterSystem.applicationIsQuitting;

        protected static Transform CurrentViewTransform(Transform defaultTransform = null) =>
            PainterSystem.CurrentViewTransform(defaultTransform);

        public virtual bool Inspect() => _uTags.Inspect();  

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
        protected static TextureMeta InspectedImageMeta { get { var ip = InspectedPainter; return ip ? ip.TexMeta : null; } }
        protected static GridNavigator Grid => GridNavigator.Inst();
        protected static MeshEditorManager MeshMGMT => MeshEditorManager.Inst;
        protected static EditableMesh EditedMesh => MeshEditorManager.editedMesh;
        protected static bool DocsEnabled => !PainterDataAndConfig.hideDocumentation;
        public static bool applicationIsQuitting;
        
        public static Transform CurrentViewTransform(Transform defaultTransform = null)
        {

            if (Application.isPlaying)
            {
                return TexMGMT.MainCamera.transform;

            }

#if UNITY_EDITOR
            else if (SceneView.lastActiveSceneView != null)
                return SceneView.lastActiveSceneView.camera.transform;
#endif


            return defaultTransform;
        }
    }
}