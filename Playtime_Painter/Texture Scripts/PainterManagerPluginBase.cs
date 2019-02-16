using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System.Collections.Generic;
using System;

namespace Playtime_Painter {


    public class PainterManagerPluginAttribute : Abstract_WithTaggedTypes
    {
        public override TaggedTypes_STD TaggedTypes => PainterManagerPluginBase.all;
    }


    public interface IPainterManagerPlugin_ComponentPEGI
    {
        bool Component_PEGI();
    }

    [PainterManagerPlugin]
    public abstract class PainterManagerPluginBase : PainterStuffKeepUnrecognized_STD, IGotDisplayName, IGotClassTag {

        public static List<PainterManagerPluginBase> plugins;

        public static List<IPainterManagerPlugin_ComponentPEGI> componentMGMTplugins = new List<IPainterManagerPlugin_ComponentPEGI>();

        public static void RefreshPlugins() {

            if (plugins == null)
                plugins = new List<PainterManagerPluginBase>();
            else
                for (var i = 0; i < plugins.Count; i++)
                    if (plugins[i] == null) { plugins.RemoveAt(i); i--; }

            componentMGMTplugins.Clear();


            foreach (var t in all)
            {
                var contains = false;

                componentMGMTplugins.TryAdd(t as IPainterManagerPlugin_ComponentPEGI);

                foreach (var p in plugins)
                    if (p.GetType() == t) { contains = true; break; }

                if (!contains)
                    plugins.Add((PainterManagerPluginBase)Activator.CreateInstance(t));

            }
        }


        #region Abstract Serialized
        public abstract string ClassTag { get; } 
        public static TaggedTypes_STD all = new TaggedTypes_STD(typeof(PainterManagerPluginBase));
        public TaggedTypes_STD AllTypes => all;
        #endregion

        public virtual string NameForDisplayPEGI => ToString();

        #region Inspector
        #if PEGI
   
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

      //  private PainterBoolPlugin _pluginsGizmoDraw;

        protected void PlugIn_PainterGizmos(PainterBoolPlugin d)
        {
            _pluginsGizmoDraw += d;
            PlaytimePainter.pluginsGizmoDraw += d;
        }

      //  Blit_Functions.PaintTexture2DMethod _tex2DPaintPlugins;
        protected void PlugInCpuBlitMethod(Blit_Functions.PaintTexture2DMethod d)
        {
            _tex2DPaintPlugins += d;
            BrushType.tex2DPaintPlugins += d;
        }
        
      //  PainterBoolPlugin _pluginNeedsGridDelegates;
        protected void PlugIn_NeedsGrid(PainterBoolPlugin d) {
            _pluginNeedsGridDelegates += d;
            GridNavigator.pluginNeedsGrid_Delegates += d;
        }

      //  private BrushConfig.BrushConfigPEGIplugin _brushConfigPEGI;
        protected void PlugIn_BrushConfigPEGI(BrushConfig.BrushConfigPEGIplugin d) {
            _brushConfigPEGI += d;
            BrushConfig.brushConfigPegies += d;
        }
        
      //  MeshToolBase.MeshToolPlugBool _showVerticesPlugs;
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