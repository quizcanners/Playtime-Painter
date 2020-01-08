using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace PlaytimePainter.ComponentModules
{

   /* public class PainterPluginAttribute : AbstractWithTaggedTypes {
        public override TaggedTypesCfg TaggedTypes => TaggedModulesList<ComponentModuleBase>.all;
    }
    
    [PainterPlugin]*/
    public abstract class ComponentModuleBase : PainterSystemCfg, IGotClassTag, IPEGI
    {

        public PlaytimePainter parentComponent;

        protected TextureMeta ImgMeta => parentComponent ? parentComponent.TexMeta : null; 

        #region Abstract Serialized
        public abstract string ClassTag { get; }
        //public TaggedTypesCfg AllTypes => TaggedModulesList<ComponentModuleBase>.all;        
        #endregion


        public virtual bool GetTexture(ShaderProperty.TextureValue field, ref Texture tex, PlaytimePainter painter) => false;
        
        public virtual void OnComponentDirty()  { }

        public virtual bool SetTextureOnMaterial(ShaderProperty.TextureValue field, TextureMeta id, PlaytimePainter painter) => false;
        
        public virtual bool UpdateTilingToMaterial(ShaderProperty.TextureValue  fieldName, PlaytimePainter painter) => false;
        
        public virtual bool UpdateTilingFromMaterial(ShaderProperty.TextureValue  fieldName, PlaytimePainter painter) => false;
        
        public virtual void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<ShaderProperty.TextureValue> dest)
        {
        }

        #region Inspector

        public virtual bool BrushConfigPEGI() => false;

        public virtual bool Inspect()
        {


            return false;
        }

        #endregion

        public virtual bool OffsetAndTileUv(RaycastHit hit, PlaytimePainter p, ref Vector2 uv) => false; 

        public virtual void Update_Brush_Parameters_For_Preview_Shader(PlaytimePainter p) { }

        public virtual void BeforeGpuStroke(PlaytimePainter painter, BrushConfig br, StrokeVector st, BrushTypes.Base type) {

        }

        public virtual void AfterGpuStroke(PlaytimePainter painter, BrushConfig br, StrokeVector st, BrushTypes.Base type) {

        }
        
        #region Encode & Decode
        public override CfgEncoder Encode() => new CfgEncoder();

        public override bool Decode(string tg, string data) => false;

        #endregion
    }
}