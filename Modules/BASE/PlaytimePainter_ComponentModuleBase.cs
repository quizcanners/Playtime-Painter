using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool.ComponentModules
{
    internal abstract class ComponentModuleBase : PainterClassCfg, IGotClassTag, ICfg, IPEGI
    {

        public PainterComponent painter;

        internal TextureMeta ImgMeta => painter ? painter.TexMeta : null; 

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

        public virtual void BeforeGpuStroke(Painter.Command.ForPainterComponent command) {} //Brush br, Stroke st, BrushTypes.Base type) { }

        public virtual void AfterGpuStroke(Painter.Command.ForPainterComponent command){} //Brush br, Stroke st, BrushTypes.Base type) {

        
        
        #region Encode & Decode
        public override CfgEncoder Encode() => new CfgEncoder();

        public override void DecodeTag(string key, CfgData data)
        {
        }
        #endregion

        #region Inspector

        public virtual void BrushConfigPEGI() { }

        public virtual void Inspect() { }
        
        #endregion
    }
}