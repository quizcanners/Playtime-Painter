using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter
{
    [System.Serializable]
    public class TerrainControlGlob : PainterPluginBase {

        public override bool getTexture(string fieldName, ref Texture tex, PlaytimePainter painter)
        {
            if ((painter.terrain != null) && (fieldName.Contains(PainterDataAndConfig.terrainControl)))
            {
                tex = painter.terrain.terrainData.alphamapTextures[fieldName[0].CharToInt()];
                return true;
            }
            return false;
        }

        public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<string> dest)
        {
            if (painter.terrain != null)
            {
                Texture[] alphamaps = painter.terrain.terrainData.alphamapTextures;
                for (int i = 0; i < alphamaps.Length; i++)
                    dest.Add(i + "_" + PainterDataAndConfig.terrainControl);
            }
        }

        public override bool UpdateTylingFromMaterial(string fieldName, PlaytimePainter painter)
        {
            if (painter.terrain != null)
            {
                if (fieldName.Contains(PainterDataAndConfig.terrainControl))
                {
                    var id = painter.ImgData;
                    id.tiling = Vector2.one;
                    id.offset = Vector2.zero;
                    return true;
                }
            }
            return false;
        }

        public override bool setTextureOnMaterial(string fieldName, ImageData id, PlaytimePainter painter)
        {
            Texture tex = id.CurrentTexture();
            if (painter.terrain != null)
            {
                if (fieldName.Contains(PainterDataAndConfig.terrainControl))
                {
                    int no = fieldName[0].CharToInt();

                    if (no == 0)
                        Shader.SetGlobalTexture(PainterDataAndConfig.terrainControl, tex);

                    painter.terrain.terrainData.alphamapTextures[no] = id.texture2D;

                    return true;
                }
            }
            return false;
        }
    }

}