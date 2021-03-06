﻿using System;
using System.Collections;
using System.Collections.Generic;
using QuizCanners.Inspect;
using PlaytimePainter.MeshEditing;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;

namespace PlaytimePainter.CameraModules {

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
        bool PlugIn_PainterGizmos(PlaytimePainter painter);
    }

    internal interface IPainterManagerModuleBrush
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

/*    public interface IPainterManagerModule_MeshToolShowVertex
    {
        void PlugIn_MeshToolShowVertex();
    }*/

    internal interface IMeshToolPlugin
    {
        bool MeshToolInspection(MeshToolBase currentTool);
       
    }
    
    public abstract class CameraModuleBase : PainterClassCfg, IGotReadOnlyName, IGotClassTag, ICfg, IPEGI_ListInspect {

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
        internal static TaggedTypesCfg all = new TaggedTypesCfg(typeof(CameraModuleBase));
        public TaggedTypesCfg AllTypes => all;
        #endregion

        #region Encode & Decode
        public override CfgEncoder Encode() => new CfgEncoder();//this.EncodeUnrecognized();

        public override void Decode(string key, CfgData data) { }
        #endregion

        public virtual string GetNameForInspector()=> ToString().SimplifyTypeName();
        
        public virtual void Update() { }

        public virtual void Enable() { }

        public virtual void Disable() {  }

        #region Inspector

        public virtual string ToolTip => "Painter plugin";

        public virtual void InspectInList(ref int edited, int ind) {

            if (GetNameForInspector().ClickLabel())
                edited = ind;

            pegi.FullWindow.DocumentationClickOpen(ToolTip);

            if (icon.Enter.Click())
                edited = ind;
        }
        
        #endregion

    }
}