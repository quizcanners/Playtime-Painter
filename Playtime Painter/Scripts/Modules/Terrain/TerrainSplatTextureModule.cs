using System.Collections.Generic;
using UnityEngine;
using QuizCannersUtilities;

namespace PlaytimePainter {

    [TaggedType(tag)]
    public class TerrainSplatTextureModule : PainterComponentModuleBase {

        const string tag = "TerSplat";
        public override string ClassTag => tag;

        public override bool GetTexture(ShaderProperty.TextureValue field, ref Texture tex, PlaytimePainter painter)
        {
            if (!painter.terrain || (!field.HasUsageTag(PainterDataAndConfig.TERRAIN_SPLAT_DIFFUSE))) return false;
            var no = field.NameForDisplayPEGI[0].CharToInt();

            var l = painter.terrain.terrainData.terrainLayers;

            if (l.Length > no)
                tex = l[no].diffuseTexture;
            
            return true;
        }

        public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<ShaderProperty.TextureValue> dest) {
            if (!painter.terrain) return;

            var sp = painter.terrain.terrainData.terrainLayers;

            for (var i = 0; i < sp.Length; i++) {
                var l = sp.TryGet(i);
                if (l != null)
                    dest.Add(new ShaderProperty.TextureValue( i + PainterDataAndConfig.TERRAIN_SPLAT_DIFFUSE + l.diffuseTexture.name, PainterDataAndConfig.TERRAIN_SPLAT_DIFFUSE));
            }

        }

        public override bool UpdateTilingFromMaterial(ShaderProperty.TextureValue  fieldName, PlaytimePainter painter)
        {
            if (!painter.terrain) return false;

            if (!fieldName.HasUsageTag(PainterDataAndConfig.TERRAIN_SPLAT_DIFFUSE)) return false;

            var no = fieldName.NameForDisplayPEGI[0].CharToInt();

            var ls = painter.terrain.terrainData.terrainLayers;

        
            if (ls.Length <= no) return true;

            var l = ls.TryGet(no);

            var terrainData = painter.terrain.terrainData;
            var width = terrainData.size.x / l.tileSize.x;
            var length = terrainData.size.z / l.tileSize.y;

            var id = painter.ImgMeta;
            id.tiling = new Vector2(width, length);
            id.offset = l.tileOffset;

            return true;
        }

        public override bool SetTextureOnMaterial(ShaderProperty.TextureValue  field, ImageMeta id, PlaytimePainter painter)
        {
            var tex = id.CurrentTexture();
            if (!painter.terrain) return false;
            if (!field.HasUsageTag(PainterDataAndConfig.TERRAIN_SPLAT_DIFFUSE)) return false;
            var no = field.NameForDisplayPEGI[0].CharToInt();
            painter.terrain.SetSplashPrototypeTexture(id.texture2D, no);
            if (tex.GetType() != typeof(Texture2D))
                Debug.Log("Can only use Texture2D for Splat Prototypes. If using regular terrain may not see changes.");
            else
            {

            #if UNITY_EDITOR
                var texImporter = ((Texture2D)tex).GetTextureImporter();
                if (texImporter == null) return true;
                var needReimport = texImporter.WasClamped();
                needReimport |= texImporter.HadNoMipmaps();

                if (needReimport)
                    texImporter.SaveAndReimport();
            #endif

            }
            return true;
        }
    }

}