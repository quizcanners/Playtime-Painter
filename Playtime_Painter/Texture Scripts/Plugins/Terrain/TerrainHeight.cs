using System.Collections;
using System.Collections.Generic;
using QuizCannersUtilities;
using UnityEngine;

namespace Playtime_Painter {

    [TaggedType(tag)]
    public class TerrainHeight : PainterComponentPluginBase
    {
        const string tag = "TerHeight";
        public override string ClassTag => tag;

        public override bool GetTexture(ShaderProperty.TextureValue field, ref Texture tex, PlaytimePainter painter) {
            if (!painter.terrain || !field.Equals(PainterDataAndConfig.TerrainHeight)) return false;
            tex = painter.terrainHeightTexture;
            return true;
        }

        public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<ShaderProperty.TextureValue> dest)
        {
            if (painter.terrain)
                dest.Add(PainterDataAndConfig.TerrainHeight);
        }

        public override bool UpdateTilingFromMaterial(ShaderProperty.TextureValue  fieldName, PlaytimePainter painter)
        {
            if (!painter.terrain) return false;
            if (!fieldName.Equals(PainterDataAndConfig.TerrainHeight)) return false;
            var id = painter.ImgMeta;
            if (id == null) return true;
            id.tiling = Vector2.one;
            id.offset = Vector2.zero;
            return true;
        }

        public override bool SetTextureOnMaterial(ShaderProperty.TextureValue  field, ImageMeta id, PlaytimePainter painter)
        {
            if (!painter.terrain) return false;
            if (!field.Equals(PainterDataAndConfig.TerrainHeight)) return false;
            if (id != null && id.texture2D)
                painter.terrainHeightTexture = id.texture2D;

            var tex = id.CurrentTexture();

            PainterDataAndConfig.TerrainHeight.GlobalValue = tex;
            return true;
        }

        public override void OnUpdate(PlaytimePainter painter) {
            if (painter.terrainHeightTexture)
                PainterDataAndConfig.TerrainHeight.GlobalValue = painter.terrainHeightTexture.GetDestinationTexture();
        }

    
    }

}
