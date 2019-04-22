using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System.Collections.Generic;
using System;
using System.Collections;

namespace Playtime_Painter {


    public class PainterManagerPluginAttribute : AbstractWithTaggedTypes
    {
        public override TaggedTypesCfg TaggedTypes => PainterSystemManagerPluginBase.all;
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

        void PaintRenderTexture(StrokeVector stroke, ImageMeta image, BrushConfig bc, PlaytimePainter painter);

        void PaintPixelsInRam(StrokeVector stroke, float brushAlpha, ImageMeta image, BrushConfig bc, PlaytimePainter painter);

        bool NeedsGrid(PlaytimePainter p);

        Shader GetPreviewShader(PlaytimePainter p);

        Shader GetBrushShaderDoubleBuffer(PlaytimePainter p);

        Shader GetBrushShaderSingleBuffer(PlaytimePainter p);

        bool IsEnabledFor(PlaytimePainter p, ImageMeta image, BrushConfig cfg);


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
    public abstract class PainterSystemManagerPluginBase : PainterSystemKeepUnrecognizedCfg, IGotDisplayName, IGotClassTag, IPEGI_ListInspect {

        public static List<PainterSystemManagerPluginBase> plugins;

        public static readonly List<IPainterManagerPluginComponentPEGI> ComponentInspectionPlugins = new List<IPainterManagerPluginComponentPEGI>();

        public static readonly List<IPainterManagerPluginBrush> BrushPlugins = new List<IPainterManagerPluginBrush>();

        public static readonly List<IPainterManagerPluginGizmis> GizmoPlugins = new List<IPainterManagerPluginGizmis>();

        public static readonly List<IPainterManagerPlugin_MeshToolShowVertex> MeshToolShowVertexPlugins = new List<IPainterManagerPlugin_MeshToolShowVertex>();

        public static readonly List<IMeshToolPlugin> MeshToolPlugins = new List<IMeshToolPlugin>();

        public static readonly List<IPainterManagerPluginOnGUI> GuiPlugins = new List<IPainterManagerPluginOnGUI>();

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

            ComponentInspectionPlugins.Clear();
            BrushPlugins.Clear();
            GizmoPlugins.Clear();
            MeshToolShowVertexPlugins.Clear();
            MeshToolPlugins.Clear();
            GuiPlugins.Clear();

            foreach (var t in plugins) {

                ComponentInspectionPlugins.TryAdd(t as IPainterManagerPluginComponentPEGI);

                BrushPlugins.TryAdd(t as IPainterManagerPluginBrush);

                GizmoPlugins.TryAdd(t as IPainterManagerPluginGizmis);

                MeshToolShowVertexPlugins.TryAdd(t as IPainterManagerPlugin_MeshToolShowVertex);

                MeshToolPlugins.TryAdd(t as IMeshToolPlugin);

                GuiPlugins.TryAdd(t as IPainterManagerPluginOnGUI);
            }
        }
        
        #region Abstract Serialized
        public abstract string ClassTag { get; } 
        public static TaggedTypesCfg all = new TaggedTypesCfg(typeof(PainterSystemManagerPluginBase));
        public TaggedTypesCfg AllTypes => all;
        #endregion

        #region Encode & Decode
        public override CfgEncoder Encode() => this.EncodeUnrecognized();

        public override bool Decode(string tg, string data) => false;
        #endregion

        public virtual string NameForDisplayPEGI => ToString();
        
        public virtual void Update() { }

        public virtual void Enable() { }

        public virtual void Disable() {  }
        
        #region Inspector

        public virtual string ToolTip => "Painter plugin";

        public virtual bool PEGI_inList(IList list, int ind, ref int edited)
        {

            if (NameForDisplayPEGI.ClickLabel())
                edited = ind;

            ToolTip.fullWindowDocumentationClick();

            if (icon.Enter.Click())
                edited = ind;

            return false;
        }

        #endregion

    }
}