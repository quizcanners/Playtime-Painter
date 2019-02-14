using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter {


    public class PainterManagerPluginAttribute : Abstract_WithTaggedTypes
    {
        public override TaggedTypes_STD TaggedTypes => PainterManagerPluginBase.all;
    }

    [PainterManagerPlugin]
    public abstract class PainterManagerPluginBase : PainterStuffKeepUnrecognized_STD, IGotDisplayName, IGotClassTag {

        #region Abstract Serialized
        public abstract string ClassTag { get; } 
        public static TaggedTypes_STD all = new TaggedTypes_STD(typeof(PainterManagerPluginBase));
        public TaggedTypes_STD AllTypes => all;
        #endregion

        public virtual string NameForDisplayPEGI => ToString();

        #region Inspector
        #if PEGI
        private pegi.CallDelegate _pluginsComponentPEGI;
        
        protected pegi.CallDelegate PlugInPainterComponent { set
            {
                _pluginsComponentPEGI += value;
                PlaytimePainter.pluginsComponentPEGI += value;
            }
        }

        private pegi.CallDelegate _vertexEdgePegiDelegates;
        protected void PlugIn_VertexEdgePEGI(pegi.CallDelegate d)
        {
            _vertexEdgePegiDelegates += d;
            VertexEdgeTool.pegiDelegates += d;
        }
        

#endif
        #endregion

        #region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized();

        public override bool Decode(string tg, string data) => false;
        #endregion

        private PainterBoolPlugin _pluginsGizmoDraw;
        protected void PlugIn_PainterGizmos(PainterBoolPlugin d)
        {
            _pluginsGizmoDraw += d;
            PlaytimePainter.pluginsGizmoDraw += d;
        }

        Blit_Functions.PaintTexture2DMethod _tex2DPaintPlugins;
        protected void PlugInCpuBlitMethod(Blit_Functions.PaintTexture2DMethod d)
        {
            _tex2DPaintPlugins += d;
            BrushType.tex2DPaintPlugins += d;
        }
        
        PainterBoolPlugin _pluginNeedsGridDelegates;
        protected void PlugIn_NeedsGrid(PainterBoolPlugin d) {
            _pluginNeedsGridDelegates += d;
            GridNavigator.pluginNeedsGrid_Delegates += d;
        }

        private BrushConfig.BrushConfigPEGIplugin _brushConfigPEGI;
        protected void PlugIn_BrushConfigPEGI(BrushConfig.BrushConfigPEGIplugin d) {
            _brushConfigPEGI += d;
            BrushConfig.brushConfigPegies += d;
        }
        
        MeshToolBase.MeshToolPlugBool _showVerticesPlugs;
        protected void PlugIn_MeshToolShowVertex(MeshToolBase.MeshToolPlugBool d) {
            _showVerticesPlugs += d;
            MeshToolBase.showVerticesPlugs += d;
        }

        public virtual void Update() { }

        public virtual void Enable() {

        }

        public virtual void Disable() {

#if PEGI
            PlaytimePainter.pluginsComponentPEGI -= _pluginsComponentPEGI;
            
            VertexEdgeTool.pegiDelegates -= _vertexEdgePegiDelegates;
#endif

            PlaytimePainter.pluginsGizmoDraw -= _pluginsGizmoDraw;
            BrushType.tex2DPaintPlugins -= _tex2DPaintPlugins;
            GridNavigator.pluginNeedsGrid_Delegates -= _pluginNeedsGridDelegates;
            BrushConfig.brushConfigPegies -= _brushConfigPEGI;
            MeshToolBase.showVerticesPlugs -= _showVerticesPlugs;
        }

        public virtual bool IsA3DBrush(PlaytimePainter painter, BrushConfig bc, ref bool overrideOther) => false; 

        public virtual bool PaintRenderTexture(StrokeVector stroke, ImageMeta image, BrushConfig bc, PlaytimePainter painter)  =>  false;
        
        public virtual Shader GetPreviewShader(PlaytimePainter p) => null; 

        public virtual Shader GetBrushShaderDoubleBuffer(PlaytimePainter p) => null; 

        public virtual Shader GetBrushShaderSingleBuffer(PlaytimePainter p) => null; 
        
    }
}