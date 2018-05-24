using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace Playtime_Painter
{

   

    [ExecuteInEditMode]
    [System.Serializable]
    public class PainterManagerPluginBase : PainterStuffMono {
#if PEGI
        PEGIcallDelegate plugins_ComponentPEGI;

        protected void PlugIn_PainterComponent(PEGIcallDelegate d) {
            plugins_ComponentPEGI += d;
            PlaytimePainter.plugins_ComponentPEGI += d;
        }
#endif

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

        #if PEGI
        PEGIcallDelegate VertexEdgePEGIdelegates;
        protected void PlugIn_VertexEdgePEGI(PEGIcallDelegate d) {
            VertexEdgePEGIdelegates += d;
            VertexEdgeTool.PEGIdelegates += d;
        }
#endif

        MeshToolBase.meshToolPlugBool showVerticesPlugs;
        protected void PlugIn_MeshToolShowVertex(MeshToolBase.meshToolPlugBool d) {
            showVerticesPlugs += d;
            MeshToolBase.showVerticesPlugs += d;
        }


        public void OnDisable() {
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

        public virtual void OnEnable()  { }

        //public virtual bool BrushConfigPEGI(ref bool overrideBlitModePEGI, BrushConfig br) { return false; }

        public virtual bool ConfigTab_PEGI() { return false; }

        public virtual bool isA3Dbrush(PlaytimePainter pntr, BrushConfig bc, ref bool overrideOther) { return false; }

        public virtual bool PaintRenderTexture(StrokeVector stroke, ImageData image, BrushConfig bc, PlaytimePainter pntr) {
          
            return false;
        }

        public virtual Shader GetPreviewShader(PlaytimePainter p) { return null; }

        public virtual Shader GetBrushShaderDoubleBuffer(PlaytimePainter p) { return null; }

        public virtual Shader GetBrushShaderSingleBuffer(PlaytimePainter p) { return null; }
    }
}