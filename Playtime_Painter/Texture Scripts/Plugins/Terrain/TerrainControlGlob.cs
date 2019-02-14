using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter
{

    [TaggedType(tag)]
    public class TerrainControlGlob : PainterComponentPluginBase {


        const string tag = "TerCol";
        public override string ClassTag => tag;

        public override bool GetTexture(string fieldName, ref Texture tex, PlaytimePainter painter)
        {
            if ((painter.terrain != null) && (fieldName.Contains(PainterDataAndConfig.TERRAIN_CONTROL_TEXTURE)))
            {
                tex = painter.terrain.terrainData.alphamapTextures[fieldName[0].CharToInt()];
                return true;
            }
            return false;
        }

        public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<string> dest)
        {
            if (painter && painter.terrain && painter.terrain.terrainData) 
                for (int i = 0; i < painter.terrain.terrainData.alphamapTextures.Length; i++)
                    dest.Add(i + "_" + PainterDataAndConfig.terrainControl);
        }

        public override bool UpdateTylingFromMaterial(string fieldName, PlaytimePainter painter)
        {
            if (painter.terrain != null)
            {
                if (fieldName.Contains(PainterDataAndConfig.TERRAIN_CONTROL_TEXTURE))
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
            Texture tex = id.CurrentTexture();
            if (painter.terrain != null)
            {
                if (fieldName.Contains(PainterDataAndConfig.TERRAIN_CONTROL_TEXTURE))
                {
                    int no = fieldName[0].CharToInt();

                    if (no == 0)
                        PainterDataAndConfig.terrainControl.GlobalValue = tex;

                    painter.terrain.terrainData.alphamapTextures[no] = id.texture2D;

                    return true;
                }
            }
            return false;
        }
    }

}