using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System.Collections.Generic;
using System;

namespace Playtime_Painter {


    public class PainterManagerPluginAttribute : AbstractWithTaggedTypes
    {
        public override TaggedTypesStd TaggedTypes => PainterSystemManagerPluginBase.all;
    }

    public interface IPainterManagerPluginOnGUI
    {
        void OnGUI();
    }

    public interface IPainterManagerPluginComponentPEGI
    {
        #if PEGI
        bool ComponentInspector();
        #endif
    }

    public interface IPainterManagerPluginGizmis
    {
        bool PlugIn_PainterGizmos(PlaytimePainter painter);
    }

    public interface IPainterManagerPluginBrush
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
    public abstract class PainterSystemManagerPluginBase : PainterSystemKeepUnrecognizedStd, IGotDisplayName, IGotClassTag {

        public static List<PainterSystemManagerPluginBase> plugins;

        public static readonly List<IPainterManagerPluginComponentPEGI> ComponentMgmtPlugins = new List<IPainterManagerPluginComponentPEGI>();

        public static readonly List<IPainterManagerPluginBrush> BrushPlugins = new List<IPainterManagerPluginBrush>();

        public static readonly List<IPainterManagerPluginGizmis> GizmoPlugins = new List<IPainterManagerPluginGizmis>();

        public static readonly List<IPainterManagerPlugin_MeshToolShowVertex> MeshToolPlugins = new List<IPainterManagerPlugin_MeshToolShowVertex>();

        public static readonly List<IMeshToolPlugin> VertexEdgePlugins = new List<IMeshToolPlugin>();

        public static readonly List<IPainterManagerPluginOnGUI> GUIplugins = new List<IPainterManagerPluginOnGUI>();

        public static void RefreshPlugins() {

            if (plugins == null)
                plugins = new List<PainterSystemManagerPluginBase>();
            else
                for (var i = 0; i < plugins.Count; i++)
                    if (plugins[i] == null) { plugins.RemoveAt(i); i--; }

          

            foreach (var t in all)
            {
                var contains = false;

             

                foreach (var p in plugins)
                    if (p.GetType() == t) { contains = true; break; }

                if (!contains)
                    plugins.Add((PainterSystemManagerPluginBase)Activator.CreateInstance(t));

            }

            ComponentMgmtPlugins.Clear();
            BrushPlugins.Clear();
            GizmoPlugins.Clear();
            MeshToolPlugins.Clear();
            VertexEdgePlugins.Clear();
            GUIplugins.Clear();

            foreach (var t in plugins) {

                ComponentMgmtPlugins.TryAdd(t as IPainterManagerPluginComponentPEGI);

                BrushPlugins.TryAdd(t as IPainterManagerPluginBrush);

                GizmoPlugins.TryAdd(t as IPainterManagerPluginGizmis);

                MeshToolPlugins.TryAdd(t as IPainterManagerPlugin_MeshToolShowVertex);

                VertexEdgePlugins.TryAdd(t as IMeshToolPlugin);

                GUIplugins.TryAdd(t as IPainterManagerPluginOnGUI);
            }
        }
        
        #region Abstract Serialized
        public abstract string ClassTag { get; } 
        public static TaggedTypesStd all = new TaggedTypesStd(typeof(PainterSystemManagerPluginBase));
        public TaggedTypesStd AllTypes => all;
        #endregion

        #region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized();

        public override bool Decode(string tg, string data) => false;
        #endregion

        public virtual string NameForDisplayPEGI => ToString();
        
        public virtual void Update() { }

        public virtual void Enable() { }

        public virtual void Disable() {  }

    }
}