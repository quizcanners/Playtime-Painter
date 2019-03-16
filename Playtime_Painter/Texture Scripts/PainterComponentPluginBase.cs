using System.Collections.Generic;
using UnityEngine;
using System;
using QuizCannersUtilities;

namespace Playtime_Painter
{
    public class PainterPluginAttribute : AbstractWithTaggedTypes {
        public override TaggedTypesCfg TaggedTypes => PainterComponentPluginBase.All;
    }
    
    [PainterPlugin]
    public class PainterComponentPluginBase : AbstractCfg, IGotClassTag {

        #region Abstract Serialized
        public virtual string ClassTag => "Override me";
        public static readonly TaggedTypesCfg All = new TaggedTypesCfg(typeof(PainterComponentPluginBase));
        public TaggedTypesCfg AllTypes => All;
        #endregion

        public static void UpdatePlugins(PlaytimePainter painter) {

           
            for (var i = 0; i < painter.plugins.Count; i++) {
                var nt = painter.plugins[i];

                if (nt != null) continue;
                painter.plugins.RemoveAt(i);
                i--;
            }
            
            foreach (var t in All) 
                if (!painter.plugins.ContainsInstanceType(t)) 
                    painter.plugins.Add((PainterComponentPluginBase)Activator.CreateInstance(t));  
            
        }

        public virtual bool GetTexture(ShaderProperty.TextureValue field, ref Texture tex, PlaytimePainter painter) => false;
        
        public virtual void OnUpdate(PlaytimePainter painter)  { }

        public virtual bool SetTextureOnMaterial(ShaderProperty.TextureValue field, ImageMeta id, PlaytimePainter painter) => false;
        
        public virtual bool UpdateTilingToMaterial(ShaderProperty.TextureValue  fieldName, PlaytimePainter painter) => false;
        
        public virtual bool UpdateTilingFromMaterial(ShaderProperty.TextureValue  fieldName, PlaytimePainter painter) => false;
        
        public virtual void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<ShaderProperty.TextureValue> dest)
        {
        }

        public virtual bool BrushConfigPEGI() => false;
        
        public virtual bool OffsetAndTileUv(RaycastHit hit, PlaytimePainter p, ref Vector2 uv) => false; 

        public virtual void Update_Brush_Parameters_For_Preview_Shader(PlaytimePainter p) { }

        public virtual void BeforeGpuStroke(PlaytimePainter painter, BrushConfig br, StrokeVector st, BrushType type) {

        }

        public virtual void AfterGpuStroke(PlaytimePainter painter, BrushConfig br, StrokeVector st, BrushType type) {

        }

        #region Encode & Decode
        public override CfgEncoder Encode() => new CfgEncoder();

        public override bool Decode(string tg, string data) => false;
        #endregion
    }
}