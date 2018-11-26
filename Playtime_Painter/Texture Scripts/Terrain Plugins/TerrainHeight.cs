using System.Collections;
using System.Collections.Generic;
using SharedTools_Stuff;
using UnityEngine;

namespace Playtime_Painter
{

    [TaggedType(tag)]
    public class TerrainHeight : PainterPluginBase
    {
        const string tag = "TerHeight";
        public override string ClassTag => tag;


        public override bool GetTexture(string fieldName, ref Texture tex, PlaytimePainter painter)
        {
            if ((painter.terrain != null) && (fieldName.Contains(PainterDataAndConfig.terrainHeight)))
            {
                tex = painter.terrainHeightTexture;
                return true;
            }
            return false;
        }

        public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<string> dest)
        {
            if (painter.terrain != null)
                dest.Add(PainterDataAndConfig.terrainHeight);
        }

        public override bool UpdateTylingFromMaterial(string fieldName, PlaytimePainter painter)
        {
            if (painter.terrain != null)
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
            Texture tex = id.CurrentTexture();
            if (painter.terrain != null)
            {
                if (fieldName.Contains(PainterDataAndConfig.terrainHeight))
                {
                    if (id != null)
                        painter.terrainHeightTexture = id.texture2D;

                    Shader.SetGlobalTexture(PainterDataAndConfig.terrainHeight, tex);
                    return true;
                }
            }
            return false;
        }

        public override void OnUpdate(PlaytimePainter painter)
        {
            if (painter.terrainHeightTexture != null)
                Shader.SetGlobalTexture(PainterDataAndConfig.terrainHeight, painter.terrainHeightTexture.GetDestinationTexture());
        }

    
    }

}
