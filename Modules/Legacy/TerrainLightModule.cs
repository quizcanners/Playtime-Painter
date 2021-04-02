using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.CfgDecode;
using QuizCanners.Utils;
using UnityEngine;

namespace PlaytimePainter.ComponentModules {

    [TaggedType(tag)]
    public class TerrainLightModule : ComponentModuleBase {
        private const string tag = "TerLight";
        public override string ClassTag => tag;

        private MergingTerrainController mergingTerrain;
        public int testData;

        private void FindMergingTerrain(PlaytimePainter painter) {

            if (!mergingTerrain && painter.terrain)
                mergingTerrain = painter.GetComponent<MergingTerrainController>();
        }

        public override bool GetTexture(ShaderProperty.TextureValue field, ref Texture tex)
        {
            if (!painter.terrain || !field.Equals(PainterShaderVariables.TerrainLight)) return false;
            
            tex = mergingTerrain.lightTexture;
            return true;
        }

        public override void GetNonMaterialTextureNames(ref List<ShaderProperty.TextureValue> dest)
        {
            FindMergingTerrain(painter);

            if (painter.terrain && mergingTerrain)
                dest.Add(PainterShaderVariables.TerrainLight);
        }

        public override bool UpdateTilingFromMaterial(ShaderProperty.TextureValue fieldName)
        {
            if (!painter.terrain || fieldName == null ||
                !fieldName.Equals(PainterShaderVariables.TerrainLight)) return false;
            
            var id = painter.TexMeta;
            if (id == null) return true;
            id.tiling = Vector2.one;
            id.offset = Vector2.zero;
            return true;

        }

        public override bool SetTextureOnMaterial(ShaderProperty.TextureValue field, TextureMeta id)
        {

            var tex = id.CurrentTexture();
            
            if (!painter.terrain || !field.Equals(PainterShaderVariables.TerrainLight)) return false;
            
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

            PainterShaderVariables.TerrainLight.GlobalValue = tex;
            
            return true;
        }

        public override void OnComponentDirty()
        {

            FindMergingTerrain(painter);

            if (!mergingTerrain) return;
            
            if (mergingTerrain.lightTexture)
                PainterShaderVariables.TerrainLight.GlobalValue = mergingTerrain.lightTexture.GetDestinationTexture();

            mergingTerrain.UpdateTextures();

        }
        
        public override bool BrushConfigPEGI()
        {
            var changed = false;
            
            FindMergingTerrain(painter);

            if (mergingTerrain)
               MergingTerrainController.PluginInspectPart().nl(ref changed);

            return changed;
        }
    }

}
