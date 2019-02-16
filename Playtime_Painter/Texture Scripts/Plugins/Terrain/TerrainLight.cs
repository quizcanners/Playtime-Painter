using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter {

    [TaggedType(tag)]
    public class TerrainLight : PainterComponentPluginBase {

        const string tag = "TerLight";
        public override string ClassTag => tag;

        private MergingTerrainController mergingTerrain;
        public int testData;
        
        void FindMergingTerrain(PlaytimePainter painter) {

            if (!mergingTerrain && painter.terrain)
                mergingTerrain = painter.GetComponent<MergingTerrainController>();
        }

        public override bool GetTexture(ShaderProperty.TextureValue field, ref Texture tex, PlaytimePainter painter)
        {
            if (!painter.terrain || !field.Equals(PainterDataAndConfig.TerrainLight)) return false;
            
            tex = mergingTerrain.lightTexture;
            return true;
        }

        public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<ShaderProperty.TextureValue> dest)
        {
            FindMergingTerrain(painter);

            if (painter.terrain && mergingTerrain)
                dest.Add(PainterDataAndConfig.TerrainLight);
        }

        public override bool UpdateTilingFromMaterial(ShaderProperty.TextureValue fieldName, PlaytimePainter painter)
        {
            if (!painter.terrain || fieldName == null ||
                !fieldName.Equals(PainterDataAndConfig.TerrainLight)) return false;
            
            var id = painter.ImgMeta;
            if (id == null) return true;
            id.tiling = Vector2.one;
            id.offset = Vector2.zero;
            return true;

        }

        public override bool SetTextureOnMaterial(ShaderProperty.TextureValue field, ImageMeta id, PlaytimePainter painter)
        {

            var tex = id.CurrentTexture();
            
            if (!painter.terrain || !field.Equals(PainterDataAndConfig.TerrainLight)) return false;
            
            FindMergingTerrain(painter);
                if (mergingTerrain  && id!= null)
                    mergingTerrain.lightTexture = id.texture2D;

#if UNITY_EDITOR

            var t2D = tex as Texture2D;
            
            if (t2D)
                {
                    var importer = (t2D).GetTextureImporter();
                    if (importer != null)
                    {
                        var needReimport = importer.WasClamped();
                        needReimport |= importer.HadNoMipmaps();

                        if (needReimport)
                            importer.SaveAndReimport();
                    }
                }
#endif

                PainterDataAndConfig.TerrainLight.GlobalValue = tex;



                return true;
        }

        public override void OnUpdate(PlaytimePainter painter)
        {

            FindMergingTerrain(painter);

            if (!mergingTerrain) return;
            
            if (mergingTerrain.lightTexture)
                PainterDataAndConfig.TerrainLight.GlobalValue = mergingTerrain.lightTexture.GetDestinationTexture();

            mergingTerrain.UpdateTextures();

        }

        public override bool BrushConfigPEGI()
        {
            var changed = false;

            var painter = PlaytimePainter.inspected;

            FindMergingTerrain(painter);

            if (mergingTerrain)
               mergingTerrain.PluginInspectPart().nl(ref changed);

            return changed;
        }

    }

}
