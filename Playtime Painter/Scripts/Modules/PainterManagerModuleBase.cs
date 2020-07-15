using System;
using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using PlaytimePainter.MeshEditing;
using QuizCannersUtilities;
using UnityEngine;

namespace PlaytimePainter.CameraModules {

    public interface IPainterManagerPluginOnGUI
    {
        void OnGUI();
    }

    public interface IPainterManagerModuleComponentPEGI
    {
        bool ComponentInspector();
    }

    public interface IPainterManagerModuleGizmis
    {
        bool PlugIn_PainterGizmos(PlaytimePainter painter);
    }

    public interface IPainterManagerModuleBrush
    {
        bool IsA3DBrush(PlaytimePainter painter, Brush bc, ref bool overrideOther);

        void PaintRenderTextureUvSpace(PaintCommand.UV command);

        void PaintPixelsInRam(PaintCommand.UV command);

        Shader GetPreviewShader(PlaytimePainter p);

        Shader GetBrushShaderDoubleBuffer(PlaytimePainter p);

        Shader GetBrushShaderSingleBuffer(PlaytimePainter p);

        bool IsEnabledFor(PlaytimePainter p, TextureMeta image, Brush cfg);
        
        bool BrushConfigPEGI(Brush br);
       
    }

    public interface IPainterManagerModule_MeshToolShowVertex
    {
        void PlugIn_MeshToolShowVertex();
    }

    public interface IMeshToolPlugin
    {
        bool MeshToolInspection(MeshToolBase currentTool);
       
    }
    
    public abstract class CameraModuleBase : PainterSystemKeepUnrecognizedCfg, IGotDisplayName, IGotClassTag, IPEGI_ListInspect {

        public static List<CameraModuleBase> modules;

        public static readonly List<IPainterManagerModuleComponentPEGI> ComponentInspectionPlugins = new List<IPainterManagerModuleComponentPEGI>();

        public static readonly List<IPainterManagerModuleBrush> BrushPlugins = new List<IPainterManagerModuleBrush>();

        public static readonly List<IPainterManagerModuleGizmis> GizmoPlugins = new List<IPainterManagerModuleGizmis>();

        public static readonly List<IPainterManagerModule_MeshToolShowVertex> MeshToolShowVertexPlugins = new List<IPainterManagerModule_MeshToolShowVertex>();

        public static readonly List<IMeshToolPlugin> MeshToolPlugins = new List<IMeshToolPlugin>();

        public static readonly List<IPainterManagerPluginOnGUI> GuiPlugins = new List<IPainterManagerPluginOnGUI>();

        public static void RefreshPlugins() {

            if (modules == null)
                modules = new List<CameraModuleBase>();
            else
                for (var i = modules.Count-1; i >=0 ; i--)
                    if (modules[i] == null)
                        modules.RemoveAt(i); 
            
            foreach (var t in all) {
                var contains = false;
                
                foreach (var m in modules)
                    if (m.GetType() == t) { contains = true; break; }

                if (!contains)
                    modules.Add((CameraModuleBase)Activator.CreateInstance(t));
            }

            ComponentInspectionPlugins.Clear();
            BrushPlugins.Clear();
            GizmoPlugins.Clear();
            MeshToolShowVertexPlugins.Clear();
            MeshToolPlugins.Clear();
            GuiPlugins.Clear();

            foreach (var t in modules) {

                ComponentInspectionPlugins.TryAdd(t as IPainterManagerModuleComponentPEGI);

                BrushPlugins.TryAdd(t as IPainterManagerModuleBrush);

                GizmoPlugins.TryAdd(t as IPainterManagerModuleGizmis);

                MeshToolShowVertexPlugins.TryAdd(t as IPainterManagerModule_MeshToolShowVertex);

                MeshToolPlugins.TryAdd(t as IMeshToolPlugin);

                GuiPlugins.TryAdd(t as IPainterManagerPluginOnGUI);
            }
        }
        
        #region Abstract Serialized
        public abstract string ClassTag { get; } 
        public static TaggedTypesCfg all = new TaggedTypesCfg(typeof(CameraModuleBase));
        public TaggedTypesCfg AllTypes => all;
        #endregion

        #region Encode & Decode
        public override CfgEncoder Encode() => this.EncodeUnrecognized();

        public override bool Decode(string tg, string data) => false;
        #endregion

        public virtual string NameForDisplayPEGI()=> ToString().SimplifyTypeName();
        
        public virtual void Update() { }

        public virtual void Enable() { }

        public virtual void Disable() {  }

        #region Inspector

        public virtual string ToolTip => "Painter plugin";

        public virtual bool InspectInList(IList list, int ind, ref int edited) {

            if (NameForDisplayPEGI().ClickLabel())
                edited = ind;

            pegi.FullWindowService.DocumentationClickOpen(ToolTip);

            if (icon.Enter.Click())
                edited = ind;

            return false;
        }
        
        #endregion

    }
}