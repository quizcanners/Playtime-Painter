using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System.Collections.Generic;
using System;

namespace Playtime_Painter {


    public class PainterManagerPluginAttribute : AbstractWithTaggedTypes
    {
        public override TaggedTypesStd TaggedTypes => PainterManagerPluginBase.all;
    }
    
    public interface IPainterManagerPlugin_ComponentPEGI
    {
        #if PEGI
        bool ComponentInspector();
        #endif
    }

    public interface IPainterManagerPlugin_Gizmis
    {
        void PlugIn_PainterGizmos(PlaytimePainter painter);
    }

    public interface IPainterManagerPlugin_Brush
    {
        bool IsA3DBrush(PlaytimePainter painter, BrushConfig bc, ref bool overrideOther);

        bool PaintRenderTexture(StrokeVector stroke, ImageMeta image, BrushConfig bc, PlaytimePainter painter);

        bool PaintPixelsInRam(StrokeVector stroke, float brushAlpha, ImageMeta image, BrushConfig bc, PlaytimePainter painter);

        bool NeedsGrid(PlaytimePainter p);

        Shader GetPreviewShader(PlaytimePainter p);

        Shader GetBrushShaderDoubleBuffer(PlaytimePainter p);

        Shader GetBrushShaderSingleBuffer(PlaytimePainter p);

        #if PEGI
        bool BrushConfigPEGI(ref bool overrideBlitMode, BrushConfig br);
        #endif
    }

    public interface IPainterManagerPlugin_MeshToolShowVertex
    {
        void PlugIn_MeshToolShowVertex();
    }

    public interface IMeshToolPlugin
    {
        #if PEGI
        bool MeshToolInspection(MeshToolBase currentTool);
        #endif
    }

    [PainterManagerPlugin]
    public abstract class PainterManagerPluginBase : PainterStuffKeepUnrecognized_STD, IGotDisplayName, IGotClassTag {

        public static List<PainterManagerPluginBase> plugins;

        public static List<IPainterManagerPlugin_ComponentPEGI> componentMGMTplugins = new List<IPainterManagerPlugin_ComponentPEGI>();

        public static List<IPainterManagerPlugin_Brush> brushPlugins = new List<IPainterManagerPlugin_Brush>();

        public static List<IPainterManagerPlugin_Gizmis> gizmoPlugins = new List<IPainterManagerPlugin_Gizmis>();

        public static List<IPainterManagerPlugin_MeshToolShowVertex> meshToolPlugins = new List<IPainterManagerPlugin_MeshToolShowVertex>();

        public static List<IMeshToolPlugin> vertexEdgePlugins = new List<IMeshToolPlugin>();

        public static void RefreshPlugins() {

            if (plugins == null)
                plugins = new List<PainterManagerPluginBase>();
            else
                for (var i = 0; i < plugins.Count; i++)
                    if (plugins[i] == null) { plugins.RemoveAt(i); i--; }

          

            foreach (var t in all)
            {
                var contains = false;

             

                foreach (var p in plugins)
                    if (p.GetType() == t) { contains = true; break; }

                if (!contains)
                    plugins.Add((PainterManagerPluginBase)Activator.CreateInstance(t));

            }

            componentMGMTplugins.Clear();
            brushPlugins.Clear();
            gizmoPlugins.Clear();
            meshToolPlugins.Clear();
            vertexEdgePlugins.Clear();

            foreach (var t in plugins) {

                componentMGMTplugins.TryAdd(t as IPainterManagerPlugin_ComponentPEGI);

                brushPlugins.TryAdd(t as IPainterManagerPlugin_Brush);

                gizmoPlugins.TryAdd(t as IPainterManagerPlugin_Gizmis);

                meshToolPlugins.TryAdd(t as IPainterManagerPlugin_MeshToolShowVertex);

                vertexEdgePlugins.TryAdd(t as IMeshToolPlugin);
            }
        }
        
#region Abstract Serialized
        public abstract string ClassTag { get; } 
        public static TaggedTypesStd all = new TaggedTypesStd(typeof(PainterManagerPluginBase));
        public TaggedTypesStd AllTypes => all;
#endregion

#region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized();

        public override bool Decode(string tg, string data) => false;
        #endregion

        public virtual string NameForDisplayPEGI => this.ToString();
        
        public virtual void Update() { }

        public virtual void Enable() { }

        public virtual void Disable() {  }

    }
}