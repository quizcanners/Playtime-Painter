using System.Collections;
using System.Collections.Generic;
using SharedTools_Stuff;
using UnityEngine;

namespace Playtime_Painter {

    [TaggedType(tag)]
    public class TerrainHeight : PainterComponentPluginBase
    {
        const string tag = "TerHeight";
        public override string ClassTag => tag;

        public override bool GetTexture(string fieldName, ref Texture tex, PlaytimePainter painter) {
            if ((painter.terrain) && (fieldName.Contains(PainterDataAndConfig.terrainHeight))) {
                tex = painter.terrainHeightTexture;
                return true;
            }
            return false;
        }

        public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<string> dest)
        {
            if (painter.terrain)
                dest.Add(PainterDataAndConfig.terrainHeight);
        }

        public override bool UpdateTylingFromMaterial(string fieldName, PlaytimePainter painter)
        {
            if (painter.terrain)
            {
                if (fieldName.Contains(PainterDataAndConfig.terrainHeight))
                {
                    var id = painter.ImgData;
                    if (id != null) {
                        id.tiling = Vector2.one;
                        id.offset = Vector2.zero;
                    }
                    return true;
                }
            }
            return false;
        }

        public override bool SetTextureOnMaterial(string fieldName, ImageData id, PlaytimePainter painter)
        {
           
            if (painter.terrain)
            {
                if (fieldName.Contains(PainterDataAndConfig.terrainHeight))
                {
                    if (id != null && id.texture2D)
                        painter.terrainHeightTexture = id.texture2D;

                    Texture tex = id.CurrentTexture();

                    Shader.SetGlobalTexture(PainterDataAndConfig.terrainHeight, tex);
                    return true;
                }
            }
            return false;
        }

        public override void OnUpdate(PlaytimePainter painter) {
            if (painter.terrainHeightTexture)
                Shader.SetGlobalTexture(PainterDataAndConfig.terrainHeight, painter.terrainHeightTexture.GetDestinationTexture());
        }

    
    }

}
