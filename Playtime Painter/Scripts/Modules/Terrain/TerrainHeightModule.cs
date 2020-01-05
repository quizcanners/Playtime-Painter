using System.Collections.Generic;
using QuizCannersUtilities;
using UnityEngine;

namespace PlaytimePainter {

    [TaggedType(Tag)]
    public class TerrainHeightModule : ComponentModuleBase
    {
        private const string Tag = "TerHeight";
        public override string ClassTag => Tag;

        private bool CorrectField(ShaderProperty.TextureValue field) => field.Equals(PainterShaderVariables.TerrainHeight);


        private bool CorrectField(ShaderProperty.TextureValue field, PlaytimePainter painter) => 
            painter.terrain &&
            field.Equals(PainterShaderVariables.TerrainHeight);


        public override bool GetTexture(ShaderProperty.TextureValue field, ref Texture tex, PlaytimePainter painter) {
            if (!CorrectField(field, painter)) return false;
            tex = painter.terrainHeightTexture;
            return true;
        }

        public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<ShaderProperty.TextureValue> dest)
        {
            if (painter.terrain)
                dest.Add(PainterShaderVariables.TerrainHeight);
        }

        public override bool UpdateTilingFromMaterial(ShaderProperty.TextureValue field, PlaytimePainter painter)
        {
            if (!CorrectField(field, painter)) return false;

            var id = painter.TexMeta;
            if (id == null) return true;
            id.tiling = Vector2.one;
            id.offset = Vector2.zero;
            return true;
        }

        public override bool SetTextureOnMaterial(ShaderProperty.TextureValue  field, TextureMeta id, PlaytimePainter painter)
        {
            if (!CorrectField(field, painter)) return false;

            if (id != null && id.texture2D)
                painter.terrainHeightTexture = id.texture2D;

            var tex = id.CurrentTexture();

            PainterShaderVariables.TerrainHeight.GlobalValue = tex;
            return true;
        }

        public override void OnUpdate(PlaytimePainter painter) {
            if (painter.terrainHeightTexture)
                PainterShaderVariables.TerrainHeight.GlobalValue = painter.terrainHeightTexture.GetDestinationTexture();
        }
    }
}
