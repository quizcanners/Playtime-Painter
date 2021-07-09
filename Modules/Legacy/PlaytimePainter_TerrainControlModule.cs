using System.Collections.Generic;
using System.Linq;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;

namespace PlaytimePainter.ComponentModules
{

    [TaggedType(CLASS_KEY)]
    internal class TerrainControlModule : ComponentModuleBase {
        private const string CLASS_KEY = "TerCol";
        public override string ClassTag => CLASS_KEY;

        public override bool GetTexture(ShaderProperty.TextureValue field, ref Texture tex)
        {
            if (!painter.terrain || !field.HasUsageTag(PainterShaderVariables.TERRAIN_CONTROL_TEXTURE)) return false;
            tex = painter.terrain.terrainData.alphamapTextures[field.GetNameForInspector()[0].CharToInt()];
            return true;
        }

        public override void GetNonMaterialTextureNames(ref List<ShaderProperty.TextureValue> dest)
        {
            if (!painter || !painter.terrain || !painter.terrain.terrainData) return;

            var d = painter.terrain.terrainData.alphamapTextures;

            dest.AddRange(d.Select((t, i) => new ShaderProperty.TextureValue(i + "_" + t.name + "_Control", PainterShaderVariables.TERRAIN_CONTROL_TEXTURE)));
        }

        public override bool UpdateTilingFromMaterial(ShaderProperty.TextureValue fieldName)
        {
            if (!painter.terrain) return false;
            if (!fieldName.HasUsageTag(PainterShaderVariables.TERRAIN_CONTROL_TEXTURE)) return false;
            var id = painter.TexMeta;
            if (id == null) return true;
            
            id.Tiling = Vector2.one;
            id.Offset = Vector2.zero;
            
            return true;
        }

        public override bool SetTextureOnMaterial(ShaderProperty.TextureValue  field, TextureMeta id)
        {
            var tex = id.CurrentTexture();
            if (!painter.terrain) return false;

            if (!field.HasUsageTag(PainterShaderVariables.TERRAIN_CONTROL_TEXTURE)) return false;
            
            var no = field.GetNameForInspector()[0].CharToInt();

            if (no == 0)
                PainterShaderVariables.TerrainControlMain.GlobalValue = tex;

            painter.terrain.terrainData.alphamapTextures[no] = id.Texture2D;

            return true;
        }


        public override void OnComponentDirty()
        {
            if (!painter)
            {
                Debug.LogError(QcLog.IsNull(painter, "On Component Dirty")); //"Parent component is null in TerrainControlModule");
                return;
            }

            var t = painter.terrain;

            if (!t) return;

#if UNITY_2018_3_OR_NEWER
            var sp = t.terrainData.terrainLayers;
#else
            SplatPrototype[] sp = t.terrainData.splatPrototypes;
#endif

            var td = painter.terrain.terrainData;
            var tds = td.size;

            if (sp.Length != 0 && sp[0] != null)
            {
                var tilingX = tds.x / sp[0].tileSize.x;
                var tilingZ = tds.z / sp[0].tileSize.y;
                PainterShaderVariables.TerrainTiling.GlobalValue = new Vector4(tilingX, tilingZ, sp[0].tileOffset.x, sp[0].tileOffset.y);

                painter.tilingY = td.size.y / sp[0].tileSize.x;
            }

            PainterShaderVariables.TerrainScale.GlobalValue = new Vector4(tds.x, tds.y, tds.z, 0.5f / td.heightmapResolution);

            painter.UpdateTerrainPosition();

            var alphaMapTextures = td.alphamapTextures;
            if (!alphaMapTextures.IsNullOrEmpty())
                PainterShaderVariables.TerrainControlMain.GlobalValue = alphaMapTextures[0].GetDestinationTexture();

        }
    }
}