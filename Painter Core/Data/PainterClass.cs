using QuizCanners.Inspect;
using PlaytimePainter.MeshEditing;
using UnityEngine;
using QuizCanners.CfgDecode;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter
{
    /*
    public abstract class PainterClassKeepUnrecognizedCfg : PainterClassCfg,  IPEGI
    {
        public UnrecognizedTagsList UnrecognizedStd { get; } = new UnrecognizedTagsList();
        
        public virtual void Inspect() => UnrecognizedStd.Nested_Inspect();
      
    }*/
    
    public abstract class PainterClassCfg : PainterClass, ICfg 
    {
        public abstract CfgEncoder Encode();



        public abstract void Decode(string key, CfgData data);
    }

    public class PainterSystemMono : MonoBehaviour, ICfg, IPEGI
    {
        #region Encode & Decode
       // private readonly UnrecognizedTagsList _uTags = new UnrecognizedTagsList();
       // public UnrecognizedTagsList UnrecognizedStd => _uTags;



        public virtual void Decode(string key, CfgData data) { }

        public virtual CfgEncoder Encode() => new CfgEncoder();//this.EncodeUnrecognized();
        #endregion

        protected static PainterDataAndConfig Cfg => PainterCamera.Data;
        protected static PainterDataAndConfig TexMgmtData => PainterCamera.Data;
        protected static PainterCamera TexMGMT => PainterCamera.Inst;
        protected static Brush GlobalBrush => TexMgmtData.Brush;
        protected static PlaytimePainter InspectedPainter => PlaytimePainter.inspected;
        protected static MeshEditorManager MeshMGMT => MeshEditorManager.Inst;
        protected static bool ApplicationIsQuitting => PainterClass.applicationIsQuitting;

        protected static Transform CurrentViewTransform(Transform defaultTransform = null) =>
            PainterClass.CurrentViewTransform(defaultTransform);

        public virtual void Inspect() { } // _uTags.Inspect();  

    }
    
    public class PainterClass {

        protected static bool InspectAdvanced => Brush.showAdvanced;
        protected static PainterDataAndConfig Cfg => PainterCamera.Data;
        protected static PainterCamera TexMGMT => PainterCamera.Inst;
        protected static Transform RtBrush => TexMGMT.brushRenderer.transform;
        protected static Mesh BrushMesh { set { TexMGMT.brushRenderer.meshFilter.mesh = value; } }
        protected static Brush InspectedBrush => Brush._inspectedBrush;
        protected static Brush GlobalBrush => Cfg.Brush;
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

            if (SceneView.lastActiveSceneView != null)
                return SceneView.lastActiveSceneView.camera.transform;
#endif


            return defaultTransform;
        }
    }
}