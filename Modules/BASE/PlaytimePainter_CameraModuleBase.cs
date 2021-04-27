using System;
using System.Collections;
using System.Collections.Generic;
using QuizCanners.Inspect;
using PainterTool.MeshEditing;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool.CameraModules {

   /* public interface IPainterManagerPluginOnGUI
    {
        void OnGUI();
    }*/

    public interface IPainterManagerModuleComponentPEGI
    {
        bool ComponentInspector();
    }

    public interface IPainterManagerModuleGizmos
    {
        void PlugIn_PainterGizmos(PainterComponent painter);
    }

    internal interface IPainterManagerModuleBrush
    {
        bool IsA3DBrush(PainterComponent painter, Brush bc, ref bool overrideOther);

        void PaintRenderTextureUvSpace(Painter.Command.Base command);

        void PaintPixelsInRam(Painter.Command.Base command);

        Shader GetPreviewShader(PainterComponent p);

        Shader GetBrushShaderDoubleBuffer(PainterComponent p);

        Shader GetBrushShaderSingleBuffer(PainterComponent p);

        bool IsEnabledFor(PainterComponent p, TextureMeta image, Brush cfg);
        
        void BrushConfigPEGI(Brush br);
       
    }

/*    public interface IPainterManagerModule_MeshToolShowVertex
    {
        void PlugIn_MeshToolShowVertex();
    }*/

    internal interface IMeshToolPlugin
    {
        void MeshToolInspection(MeshToolBase currentTool);
       
    }
    
    public abstract class CameraModuleBase : PainterClassCfg, IGotClassTag, ICfg, IPEGI_ListInspect {

        internal static List<CameraModuleBase> modules;

        internal static readonly List<IPainterManagerModuleComponentPEGI> ComponentInspectionPlugins = new List<IPainterManagerModuleComponentPEGI>();

        internal static readonly List<IPainterManagerModuleBrush> BrushPlugins = new List<IPainterManagerModuleBrush>();

        internal static readonly List<IPainterManagerModuleGizmos> GizmoPlugins = new List<IPainterManagerModuleGizmos>();

        internal static readonly List<IMeshToolPlugin> MeshToolPlugins = new List<IMeshToolPlugin>();

        internal static void RefreshModules() {

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
            //MeshToolShowVertexPlugins.Clear();
            MeshToolPlugins.Clear();
            //GuiPlugins.Clear();

            foreach (var t in modules) {

                ComponentInspectionPlugins.TryAdd(t as IPainterManagerModuleComponentPEGI);

                BrushPlugins.TryAdd(t as IPainterManagerModuleBrush);

                GizmoPlugins.TryAdd(t as IPainterManagerModuleGizmos);

                //MeshToolShowVertexPlugins.TryAdd(t as IPainterManagerModule_MeshToolShowVertex);

                MeshToolPlugins.TryAdd(t as IMeshToolPlugin);

                //GuiPlugins.TryAdd(t as IPainterManagerPluginOnGUI);
            }
        }
        
        #region Abstract Serialized
        public abstract string ClassTag { get; }

        internal static TaggedTypes.DerrivedList all = TaggedTypes<CameraModuleBase>.DerrivedList;//new TaggedTypesCfg(typeof(CameraModuleBase));
        public TaggedTypes.DerrivedList AllTypes => all;
        #endregion

        #region Encode & Decode
        public override CfgEncoder Encode() => new CfgEncoder();//this.EncodeUnrecognized();

        public override void DecodeTag(string key, CfgData data) { }
        #endregion

        public override string ToString() => ToString().SimplifyTypeName();
        
        public virtual void Update() { }

        public virtual void Enable() { }

        public virtual void Disable() {  }

        #region Inspector

        public virtual string ToolTip => "Painter plugin";

        public virtual void InspectInList(ref int edited, int ind) {

            if (ToString().PegiLabel().ClickLabel())
                edited = ind;

            pegi.FullWindow.DocumentationClickOpen(ToolTip);

            if (Icon.Enter.Click())
                edited = ind;
        }
        
        #endregion

    }
}