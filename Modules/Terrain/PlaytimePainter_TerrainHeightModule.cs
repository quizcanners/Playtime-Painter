using System.Collections.Generic;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;

namespace PlaytimePainter.ComponentModules {

    [TaggedType(Tag)]
    internal class TerrainHeightModule : ComponentModuleBase
    {
        private const string Tag = "TerHeight";
        public override string ClassTag => Tag;


        private bool CorrectField(ShaderProperty.TextureValue field, PlaytimePainter pntr) => 
            pntr.terrain &&
            field.Equals(PainterShaderVariables.TerrainHeight);


        public override bool GetTexture(ShaderProperty.TextureValue field, ref Texture tex) {
            if (!CorrectField(field, painter)) return false;
            tex = painter.terrainHeightTexture;
            return true;
        }

        public override void GetNonMaterialTextureNames(ref List<ShaderProperty.TextureValue> dest)
        {
            if (painter.terrain)
                dest.Add(PainterShaderVariables.TerrainHeight);
        }

        public override bool UpdateTilingFromMaterial(ShaderProperty.TextureValue field)
        {
            if (!CorrectField(field, painter)) return false;

            var id = painter.TexMeta;
            if (id == null) return true;
            id.Tiling = Vector2.one;
            id.Offset = Vector2.zero;
            return true;
        }

        public override bool SetTextureOnMaterial(ShaderProperty.TextureValue  field, TextureMeta id)
        {
            if (!CorrectField(field, painter)) return false;

            if (id != null && id.Texture2D)
                painter.terrainHeightTexture = id.Texture2D;

            var tex = id.CurrentTexture();

            PainterShaderVariables.TerrainHeight.GlobalValue = tex;
            return true;
        }

        public override void OnComponentDirty() {
            if (painter.terrainHeightTexture)
            {
                PainterShaderVariables.TerrainHeight.GlobalValue = painter.terrainHeightTexture.GetDestinationTexture();
            }
        }
    }
}
