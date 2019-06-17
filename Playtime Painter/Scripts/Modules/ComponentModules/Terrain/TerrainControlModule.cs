using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using QuizCannersUtilities;

namespace PlaytimePainter
{

    [TaggedType(tag)]
    public class TerrainControlModule : PainterComponentModuleBase {


        const string tag = "TerCol";
        public override string ClassTag => tag;

        public override bool GetTexture(ShaderProperty.TextureValue field, ref Texture tex, PlaytimePainter painter)
        {
            if (!painter.terrain || !field.HasUsageTag(PainterDataAndConfig.TERRAIN_CONTROL_TEXTURE)) return false;
            tex = painter.terrain.terrainData.alphamapTextures[field.NameForDisplayPEGI()[0].CharToInt()];
            return true;
        }

        public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<ShaderProperty.TextureValue> dest)
        {
            if (!painter || !painter.terrain || !painter.terrain.terrainData) return;

            var d = painter.terrain.terrainData.alphamapTextures;

            dest.AddRange(d.Select((t, i) => new ShaderProperty.TextureValue(i + "_" + t.name + PainterDataAndConfig.TERRAIN_CONTROL_TEXTURE, PainterDataAndConfig.TERRAIN_CONTROL_TEXTURE)));
        }

        public override bool UpdateTilingFromMaterial(ShaderProperty.TextureValue fieldName, PlaytimePainter painter)
        {
            if (!painter.terrain) return false;
            if (!fieldName.HasUsageTag(PainterDataAndConfig.TERRAIN_CONTROL_TEXTURE)) return false;
            var id = painter.ImgMeta;
            if (id == null) return true;
            
            id.tiling = Vector2.one;
            id.offset = Vector2.zero;
            
            return true;
        }

        public override bool SetTextureOnMaterial(ShaderProperty.TextureValue  field, ImageMeta id, PlaytimePainter painter)
        {
            var tex = id.CurrentTexture();
            if (!painter.terrain) return false;

            if (!field.HasUsageTag(PainterDataAndConfig.TERRAIN_CONTROL_TEXTURE)) return false;
            
            var no = field.NameForDisplayPEGI()[0].CharToInt();

            if (no == 0)
                PainterDataAndConfig.TerrainControlMain.GlobalValue = tex;

            painter.terrain.terrainData.alphamapTextures[no] = id.texture2D;

            return true;
        }


        public override void OnUpdate(PlaytimePainter painter)
        {
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
                PainterDataAndConfig.TerrainTiling.GlobalValue = new Vector4(tilingX, tilingZ, sp[0].tileOffset.x, sp[0].tileOffset.y);

                painter.tilingY = td.size.y / sp[0].tileSize.x;
            }

            PainterDataAndConfig.TerrainScale.GlobalValue = new Vector4(tds.x, tds.y, tds.z, 0.5f / td.heightmapResolution);

            painter.UpdateTerrainPosition();

            var alphaMapTextures = td.alphamapTextures;
            if (!alphaMapTextures.IsNullOrEmpty())
                PainterDataAndConfig.TerrainControlMain.GlobalValue = alphaMapTextures[0].GetDestinationTexture();

        }

    }

}