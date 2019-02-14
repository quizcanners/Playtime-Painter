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

        public override bool GetTexture(string fieldName, ref Texture tex, PlaytimePainter painter) {
            if ((painter.terrain) && (fieldName.Contains(PainterDataAndConfig.TERRAIN_HEIGHT_TEXTURE))) {
                tex = painter.terrainHeightTexture;
                return true;
            }
            return false;
        }

        public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<string> dest)
        {
            if (painter.terrain)
                dest.Add(PainterDataAndConfig.TERRAIN_HEIGHT_TEXTURE);
        }

        public override bool UpdateTylingFromMaterial(string fieldName, PlaytimePainter painter)
        {
            if (painter.terrain)
            {
                if (fieldName.Contains(PainterDataAndConfig.TERRAIN_HEIGHT_TEXTURE))
                {
                    var id = painter.ImgMeta;
                    if (id != null) {
                        id.tiling = Vector2.one;
                        id.offset = Vector2.zero;
                    }
                    return true;
                }
            }
            return false;
        }

        public override bool SetTextureOnMaterial(string fieldName, ImageMeta id, PlaytimePainter painter)
        {
           
            if (painter.terrain)
            {
                if (fieldName.Contains(PainterDataAndConfig.TERRAIN_HEIGHT_TEXTURE))
                {
                    if (id != null && id.texture2D)
                        painter.terrainHeightTexture = id.texture2D;

                    Texture tex = id.CurrentTexture();

                    PainterDataAndConfig.terrainHeight.GlobalValue = tex;
                    return true;
                }
            }
            return false;
        }

        public override void OnUpdate(PlaytimePainter painter) {
            if (painter.terrainHeightTexture)
                PainterDataAndConfig.terrainHeight.GlobalValue = painter.terrainHeightTexture.GetDestinationTexture();
        }

    
    }

}
