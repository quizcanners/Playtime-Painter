using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playtime_Painter
{
    [System.Serializable]
    public class TerrainHeight : PainterPluginBase
    {

        public override bool getTexture(string fieldName, ref Texture tex, PlaytimePainter painter)
        {
            if ((painter.terrain != null) && (fieldName.Contains(PainterConfig.terrainHeight)))
            {
                tex = painter.terrainHeightTexture;
                return true;
            }
            return false;
        }

        public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<string> dest)
        {
            if (painter.terrain != null)
                dest.Add(PainterConfig.terrainHeight);
        }

        public override bool UpdateTylingFromMaterial(string fieldName, PlaytimePainter painter)
        {
            if (painter.terrain != null)
            {
                if (fieldName.Contains(PainterConfig.terrainHeight))
                {
                    painter.curImgData.tiling = Vector2.one;
                    painter.curImgData.offset = Vector2.zero;
                    return true;
                }
            }
            return false;
        }

        public override bool setTextureOnMaterial(string fieldName, imgData id, PlaytimePainter painter)
        {
            Texture tex = id.currentTexture();
            if (painter.terrain != null)
            {
                if (fieldName.Contains(PainterConfig.terrainHeight))
                {
                    if (id != null)
                        painter.terrainHeightTexture = id.texture2D;

                    Shader.SetGlobalTexture(PainterConfig.terrainHeight, tex);
                    return true;
                }
            }
            return false;
        }

        public override void OnUpdate(PlaytimePainter painter)
        {
            if (painter.terrainHeightTexture != null)
                Shader.SetGlobalTexture(PainterConfig.terrainHeight, painter.terrainHeightTexture.getDestinationTexture());
        }
    }

}
