using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

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

        public virtual string NameForPEGIdisplay => ToString();

        #region Inspector
#if PEGI
        pegi.CallDelegate plugins_ComponentPEGI;
        
        protected pegi.CallDelegate PlugIn_PainterComponent { set
            {
                plugins_ComponentPEGI += value;
                PlaytimePainter.plugins_ComponentPEGI += value;
            }
        }
        
        pegi.CallDelegate VertexEdgePEGIdelegates;
        protected void PlugIn_VertexEdgePEGI(pegi.CallDelegate d)
        {
            VertexEdgePEGIdelegates += d;
            VertexEdgeTool.PEGIdelegates += d;
        }
        

#endif
        #endregion

        #region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized();

        public override bool Decode(string tag, string data) => false;
        #endregion

        PainterBoolPlugin plugins_GizmoDraw;
        protected void PlugIn_PainterGizmos(PainterBoolPlugin d)
        {
            plugins_GizmoDraw += d;
            PlaytimePainter.plugins_GizmoDraw += d;
        }

        Blit_Functions.PaintTexture2DMethod tex2DPaintPlugins;
        protected void PlugIn_CPUblitMethod(Blit_Functions.PaintTexture2DMethod d)
        {
            tex2DPaintPlugins += d;
            BrushType.tex2DPaintPlugins += d;
        }
        
        PainterBoolPlugin pluginNeedsGrid_Delegates;
        protected void PlugIn_NeedsGrid(PainterBoolPlugin d) {
            pluginNeedsGrid_Delegates += d;
            GridNavigator.pluginNeedsGrid_Delegates += d;
        }

        BrushConfig.BrushConfigPEGIplugin brushConfigPagies;
        protected void PlugIn_BrushConfigPEGI(BrushConfig.BrushConfigPEGIplugin d) {
            brushConfigPagies += d;
            BrushConfig.brushConfigPegies += d;
        }
        
        MeshToolBase.meshToolPlugBool showVerticesPlugs;
        protected void PlugIn_MeshToolShowVertex(MeshToolBase.meshToolPlugBool d) {
            showVerticesPlugs += d;
            MeshToolBase.showVerticesPlugs += d;
        }

        public virtual void Enable() {

        }

        public virtual void Disable() {

#if PEGI
            PlaytimePainter.plugins_ComponentPEGI -= plugins_ComponentPEGI;
            VertexEdgeTool.PEGIdelegates -= VertexEdgePEGIdelegates;
#endif

            PlaytimePainter.plugins_GizmoDraw -= plugins_GizmoDraw;
            BrushType.tex2DPaintPlugins -= tex2DPaintPlugins;
            GridNavigator.pluginNeedsGrid_Delegates -= pluginNeedsGrid_Delegates;
            BrushConfig.brushConfigPegies -= brushConfigPagies;
            MeshToolBase.showVerticesPlugs -= showVerticesPlugs;
        }

        public override void Decode(string data)
        {
            base.Decode(data);
        }

        public virtual bool IsA3Dbrush(PlaytimePainter pntr, BrushConfig bc, ref bool overrideOther) { return false; }

        public virtual bool PaintRenderTexture(StrokeVector stroke, ImageData image, BrushConfig bc, PlaytimePainter pntr)  =>  false;
        
        public virtual Shader GetPreviewShader(PlaytimePainter p) { return null; }

        public virtual Shader GetBrushShaderDoubleBuffer(PlaytimePainter p) { return null; }

        public virtual Shader GetBrushShaderSingleBuffer(PlaytimePainter p) { return null; }
        
    }
}