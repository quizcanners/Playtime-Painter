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

        public virtual CfgEncoder Encode() => new();//this.EncodeUnrecognized();
        #endregion

        protected static Brush GlobalBrush => Painter.Data.Brush;
        protected static PainterComponent InspectedPainter => PainterComponent.inspected;

        protected static Transform CurrentViewTransform(Transform defaultTransform = null) =>
            PainterClass.CurrentViewTransform(defaultTransform);

    }
    
    public class PainterClass {

        protected static bool InspectAdvanced => Brush.showAdvanced;
     
        protected static Transform RtBrush => Painter.Camera.brushRenderer.transform;
        protected static Mesh BrushMesh { set { Painter.Camera.brushRenderer.meshFilter.mesh = value; } }
        protected static Brush InspectedBrush => Brush._inspectedBrush;
        protected static Brush GlobalBrush => Painter.Data.Brush;
        protected static PainterComponent InspectedPainter => PainterComponent.inspected;
        internal static TextureMeta InspectedImageMeta { get { var ip = InspectedPainter; return ip ? ip.TexMeta : null; } }
        protected static GridNavigator Grid => MeshPainting.Grid;
        internal static MeshData EditedMesh => MeshEditorManager.editedMesh;
        protected static bool DocsEnabled => !SO_PainterDataAndConfig.hideDocumentation;
       
        
        public static Transform CurrentViewTransform(Transform defaultTransform = null)
        {

            if (Application.isPlaying)
            {
                return Painter.Camera.MainCamera ? Painter.Camera.MainCamera.transform : defaultTransform;
            }

#if UNITY_EDITOR

            if (UnityEditor.SceneView.lastActiveSceneView != null)
                return UnityEditor.SceneView.lastActiveSceneView.camera.transform;
#endif


            return defaultTransform;
        }
    }
}