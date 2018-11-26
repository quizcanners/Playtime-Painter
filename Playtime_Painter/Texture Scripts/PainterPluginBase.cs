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

namespace Playtime_Painter
{
    public class PainterPluginAttribute : Abstract_WithTaggedTypes {
        public override TaggedTypes_STD TaggedTypes => PainterPluginBase.all;
    }

    [Serializable]
    public abstract class PainterPluginBase : PainterStuffKeepUnrecognized_STD, IGotClassTag {

        #region Abstract Serialized
        public abstract string ClassTag { get; }
        public static TaggedTypes_STD all = new TaggedTypes_STD(typeof(PainterPluginBase));
        public TaggedTypes_STD AllTypes => all;
        #endregion

        [SerializeField]
        public PlaytimePainter parentPainter;

        public static void UpdateList(PlaytimePainter pntr) {

            for (int i = 0; i < pntr.Plugins.Count; i++) {
                var nt = pntr.Plugins[i];

                if (nt == null) {
                    pntr.Plugins.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < pntr.Plugins.Count; i++)
                if (pntr.Plugins[i] == null) { pntr.Plugins.RemoveAt(i); i--; }

            foreach (Type t in all) {
                if (!pntr.Plugins.ContainsInstanceType(t)) 
                    pntr.Plugins.Add((PainterPluginBase)Activator.CreateInstance(t));  
            }
        }

        public virtual bool GetTexture(string fieldName, ref Texture tex, PlaytimePainter painter)
        {
            //Debug.Log("Get Texture on " + this.GetType() + "  not implemented");
            return false;
        }

        public virtual void OnUpdate(PlaytimePainter painter)  { }

        public virtual bool SetTextureOnMaterial(string fieldName, ImageData id, PlaytimePainter painter) => false;
        
        public virtual bool UpdateTylingToMaterial(string fieldName, PlaytimePainter painter) => false;
        
        public virtual bool UpdateTylingFromMaterial(string fieldName, PlaytimePainter painter) => false;
        
        public virtual void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<string> dest)
        {
            //Debug.Log("Get Names on " + this.GetType() + "  not implemented");
        }

        public virtual bool BrushConfigPEGI()
        {
            return false;

        }

        public virtual bool OffsetAndTileUV(RaycastHit hit, PlaytimePainter p, ref Vector2 uv) { return false; }

        public virtual void Update_Brush_Parameters_For_Preview_Shader(PlaytimePainter p) { }

        public virtual void BeforeGPUStroke(PlaytimePainter pntr, BrushConfig br, StrokeVector st, BrushType type) {

        }

        public virtual void AfterGPUStroke(PlaytimePainter pntr, BrushConfig br, StrokeVector st, BrushType type) {

        }

        #region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized();

        public override bool Decode(string tag, string data) => false;
        #endregion
    }
}