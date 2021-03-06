﻿using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;

namespace PlaytimePainter.ComponentModules {

    [TaggedType(CLASS_KEY)]
    internal class TerrainLightModule : ComponentModuleBase {
        private const string CLASS_KEY = "TerLight";
        public override string ClassTag => CLASS_KEY;

        private MergingTerrainController mergingTerrain;

        private void FindMergingTerrain(PlaytimePainter forPainter) {

            if (!mergingTerrain && forPainter.terrain)
                mergingTerrain = forPainter.GetComponent<MergingTerrainController>();
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
            id.Tiling = Vector2.one;
            id.Offset = Vector2.zero;
            return true;

        }

        public override bool SetTextureOnMaterial(ShaderProperty.TextureValue field, TextureMeta id)
        {

            var tex = id.CurrentTexture();
            
            if (!painter.terrain || !field.Equals(PainterShaderVariables.TerrainLight)) return false;
            
            FindMergingTerrain(painter);
                if (mergingTerrain  && id!= null)
                    mergingTerrain.lightTexture = id.Texture2D;

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
               MergingTerrainController.PluginInspectPart().nl();

            return changed;
        }
    }

}
