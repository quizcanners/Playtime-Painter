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

        public PlaytimePainter painter;

        protected TextureMeta ImgMeta => painter ? painter.TexMeta : null; 

        #region Abstract Serialized
        public abstract string ClassTag { get; }    
        #endregion


        public virtual bool GetTexture(ShaderProperty.TextureValue field, ref Texture tex) => false;
        
        public virtual void OnComponentDirty()  { }

        public virtual bool SetTextureOnMaterial(ShaderProperty.TextureValue field, TextureMeta id) => false;
        
        public virtual bool UpdateTilingFromMaterial(ShaderProperty.TextureValue  fieldName) => false;
        
        public virtual void GetNonMaterialTextureNames(ref List<ShaderProperty.TextureValue> dest) { }
        
        public virtual bool OffsetAndTileUv(RaycastHit hit, ref Vector2 uv) => false; 

        public virtual void Update_Brush_Parameters_For_Preview_Shader() { }

        public virtual void BeforeGpuStroke(PaintCommand.UV command) {} //Brush br, Stroke st, BrushTypes.Base type) { }

        public virtual void AfterGpuStroke(PaintCommand.UV command){} //Brush br, Stroke st, BrushTypes.Base type) {

        
        
        #region Encode & Decode
        public override CfgEncoder Encode() => new CfgEncoder();

        public override bool Decode(string tg, string data) => true;
        #endregion

        #region Inspector

        public virtual bool BrushConfigPEGI() => false;

        public virtual bool Inspect() => false;
        
        #endregion
    }
}