using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter
{

   

    [ExecuteInEditMode]
    [System.Serializable]
    public class PainterManagerPluginBase : PainterStuffMono, IKeepUnrecognizedSTD 
#if PEGI
        , IPEGI, IGotDisplayName
#endif
        
    {
#if PEGI
        pegi.CallDelegate plugins_ComponentPEGI;

        public virtual string NameForPEGIdisplay()
        {
            return ToString();
        }

        protected pegi.CallDelegate PlugIn_PainterComponent { set
            {
                plugins_ComponentPEGI += value;
                PlaytimePainter.plugins_ComponentPEGI += value;
            }
        }
#endif

        UnrecognizedSTD uTags = new UnrecognizedSTD();
        public UnrecognizedSTD UnrecognizedSTD => uTags;

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
        pegi.CallDelegate VertexEdgePEGIdelegates;
        protected void PlugIn_VertexEdgePEGI(pegi.CallDelegate d) {
            VertexEdgePEGIdelegates += d;
            VertexEdgeTool.PEGIdelegates += d;
        }
#endif

        MeshToolBase.meshToolPlugBool showVerticesPlugs;
        protected void PlugIn_MeshToolShowVertex(MeshToolBase.meshToolPlugBool d) {
            showVerticesPlugs += d;
            MeshToolBase.showVerticesPlugs += d;
        }


        public virtual void OnDisable() {
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
        
        public virtual bool IsA3Dbrush(PlaytimePainter pntr, BrushConfig bc, ref bool overrideOther) { return false; }

        public virtual bool PaintRenderTexture(StrokeVector stroke, ImageData image, BrushConfig bc, PlaytimePainter pntr) {
          
            return false;
        }

        public virtual Shader GetPreviewShader(PlaytimePainter p) { return null; }

        public virtual Shader GetBrushShaderDoubleBuffer(PlaytimePainter p) { return null; }

        public virtual Shader GetBrushShaderSingleBuffer(PlaytimePainter p) { return null; }
        
     

#if PEGI

        public virtual bool ConfigTab_PEGI() { "Nothing here".nl(); return false; }

        public static int inspectedUnrecognized = -1;
        public virtual bool PEGI()
        {
            bool changed =  ConfigTab_PEGI();
            changed |= uTags.PEGI();
            return changed;
        }
#endif
        public virtual ISTD Decode(string data) => this;

        public virtual bool Decode(string tag, string data) => true;

        public virtual StdEncoder Encode() => this.EncodeUnrecognized();
    }
}