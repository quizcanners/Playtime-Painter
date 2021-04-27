using QuizCanners.Inspect;
using PainterTool.MeshEditing;
using UnityEngine;
using QuizCanners.Migration;
using QuizCanners.Utils;

namespace PainterTool
{    
    public abstract class PainterClassCfg : PainterClass, ICfg 
    {
        public abstract CfgEncoder Encode();



        public abstract void DecodeTag(string key, CfgData data);
    }

    public class PainterSystemMono : Singleton.BehaniourBase, ICfg, IPEGI
    {
        #region Encode & Decode
       // private readonly UnrecognizedTagsList _uTags = new UnrecognizedTagsList();
       // public UnrecognizedTagsList UnrecognizedStd => _uTags;



        public virtual void DecodeTag(string key, CfgData data) { }

        public virtual CfgEncoder Encode() => new CfgEncoder();//this.EncodeUnrecognized();
        #endregion

        protected static SO_PainterDataAndConfig Cfg => Singleton_PainterCamera.Data;
        protected static SO_PainterDataAndConfig TexMgmtData => Singleton_PainterCamera.Data;
        protected static Singleton_PainterCamera TexMGMT => Singleton_PainterCamera.GetOrCreate();
        protected static Brush GlobalBrush => TexMgmtData.Brush;
        protected static PainterComponent InspectedPainter => PainterComponent.inspected;
        internal static MeshEditorManager MeshMGMT => MeshEditorManager.Inst;
        protected static bool ApplicationIsQuitting => PainterClass.applicationIsQuitting;

        protected static Transform CurrentViewTransform(Transform defaultTransform = null) =>
            PainterClass.CurrentViewTransform(defaultTransform);

    }
    
    public class PainterClass {

        protected static bool InspectAdvanced => Brush.showAdvanced;
        protected static SO_PainterDataAndConfig Cfg => Singleton_PainterCamera.Data;
        protected static Singleton_PainterCamera TexMGMT => Singleton_PainterCamera.GetOrCreate();
        protected static Transform RtBrush => TexMGMT.brushRenderer.transform;
        protected static Mesh BrushMesh { set { TexMGMT.brushRenderer.meshFilter.mesh = value; } }
        protected static Brush InspectedBrush => Brush._inspectedBrush;
        protected static Brush GlobalBrush => Cfg.Brush;
        protected static PainterComponent InspectedPainter => PainterComponent.inspected;
        internal static TextureMeta InspectedImageMeta { get { var ip = InspectedPainter; return ip ? ip.TexMeta : null; } }
        protected static GridNavigator Grid => GridNavigator.GetOrCreate;
        internal static MeshEditorManager MeshMGMT => MeshEditorManager.Inst;
        internal static MeshData EditedMesh => MeshEditorManager.editedMesh;
        protected static bool DocsEnabled => !SO_PainterDataAndConfig.hideDocumentation;
        public static bool applicationIsQuitting;
        
        public static Transform CurrentViewTransform(Transform defaultTransform = null)
        {

            if (Application.isPlaying)
            {
                return TexMGMT.MainCamera ? TexMGMT.MainCamera.transform : defaultTransform;
            }

#if UNITY_EDITOR

            if (UnityEditor.SceneView.lastActiveSceneView != null)
                return UnityEditor.SceneView.lastActiveSceneView.camera.transform;
#endif


            return defaultTransform;
        }
    }
}