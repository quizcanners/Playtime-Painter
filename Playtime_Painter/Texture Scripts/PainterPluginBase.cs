using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using PlayerAndEditorGUI;
using SharedTools_Stuff;
// This is a base class to use when managing special cases, such as: Terrain ControlMaps, Global Textures, Textures that are RAM only.

namespace Playtime_Painter
{

    // Inherit this base class to create textures that are not not part of the material. (GlobalShaderValues, Terrain Textures)

    [Serializable]
    public class PainterPluginBase : PainterStuffScriptable 
    {

        [SerializeField]
        public PlaytimePainter parentPainter;

        static List<Type> allTypes;

        public static void updateList(PlaytimePainter pntr)
        {

            // List<PainterPluginBase> lst,

            if (allTypes == null)
                allTypes = CsharpFuncs.GetAllChildTypesOf<PainterPluginBase>();

            for (int i = 0; i < pntr.plugins.Count; i++)
            {
                var nt = pntr.plugins[i];

                if (nt == null) {
                    pntr.plugins.RemoveAt(i);
                    i--;
                }
            }

            if (pntr.plugins.Count == 0)
            {
                foreach (Type t in allTypes)
                {
                    var obj = (PainterPluginBase)ScriptableObject.CreateInstance(t);
                  
                    pntr.plugins.Add(obj);
                }

#if UNITY_EDITOR
                foreach (var p in pntr.plugins)
                    Undo.RegisterCreatedObjectUndo(p, "plgns");
#endif

                return;
            }

            //Debug.Log("lst was not null");


            for (int i = 0; i < pntr.plugins.Count; i++)
                if (pntr.plugins[i] == null) { pntr.plugins.RemoveAt(i); i--; Debug.Log("Removing missing dataa"); }

            foreach (Type t in allTypes)
            {
                if (!pntr.plugins.ContainsInstanceType(t))
                {
                    Debug.Log("Creating instance of " + t.ToString());
                    var np = (PainterPluginBase)ScriptableObject.CreateInstance(t);
                  
                    pntr.plugins.Add(np);  //(PainterPluginBase)Activator.CreateInstance(t));
#if UNITY_EDITOR
                    Undo.RegisterCreatedObjectUndo(np, "plgns");
#endif
                }
            }


        }

        public virtual bool getTexture(string fieldName, ref Texture tex, PlaytimePainter painter)
        {
            //Debug.Log("Get Texture on " + this.GetType() + "  not implemented");
            return false;
        }

        public virtual void OnUpdate(PlaytimePainter painter)
        {

        }

        public virtual bool setTextureOnMaterial(string fieldName, ImageData id, PlaytimePainter painter)
        {
            //Debug.Log("Set Texture  on " + this.GetType() + "  not implemented");
            return false;
        }

        public virtual bool UpdateTylingToMaterial(string fieldName, PlaytimePainter painter)
        {
            // Debug.Log("Update Tiling on " + this.GetType() + " not implemented");
            return false;
        }

        public virtual bool UpdateTylingFromMaterial(string fieldName, PlaytimePainter painter)
        {
            //Debug.Log("Update Tiling on "+ this.GetType() +" not implemented");
            return false;
        }

        public virtual void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<string> dest)
        {
            //Debug.Log("Get Names on " + this.GetType() + "  not implemented");
        }

        public virtual bool BrushConfigPEGI()
        {
            return false;

        }

        public virtual bool offsetAndTileUV(RaycastHit hit, PlaytimePainter p, ref Vector2 uv) { return false; }

        public virtual void Update_Brush_Parameters_For_Preview_Shader(PlaytimePainter p) { }

        //public virtual bool PaintTexture2D(StrokeVector stroke, float brushAlpha, ImageData p, BrushConfig bc, PlaytimePainter pntr) { return false; }

        public virtual void BeforeGPUStroke(PlaytimePainter pntr, BrushConfig br, StrokeVector st, BrushType type) {

        }

        public virtual void AfterGPUStroke(PlaytimePainter pntr, BrushConfig br, StrokeVector st, BrushType type) {

        }

    }
}