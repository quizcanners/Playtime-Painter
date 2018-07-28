using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;


namespace Playtime_Painter
{
    [System.Serializable]
    public class TerrainLight : PainterPluginBase
    {
    
        public MergingTerrainController mergingTerrain;
        public int testData;

        public override bool BrushConfigPEGI()
        {
            bool changed = false;
           // if (pntr.terrain != null)
             //   "Found the terrain, yey!!".nl();
          //  changed |= "Test: ".edit(ref testData).nl();

            if (changed)
                UnityHelperFunctions.SetToDirty(this);

            return changed;
        }

        void FindMergingTerrain(PlaytimePainter pntr)
        {
            if ((mergingTerrain == null) && (pntr.terrain != null))
                mergingTerrain = pntr.GetComponent<MergingTerrainController>();
        }

        public override bool getTexture(string fieldName, ref Texture tex, PlaytimePainter pntr)
        {
            if ((pntr.terrain != null) && (fieldName.Contains(PainterDataAndConfig.terrainLight)))
            {
                tex = mergingTerrain.lightTexture;
                return true;
            }
            return false;
        }

        public override void GetNonMaterialTextureNames(PlaytimePainter pntr, ref List<string> dest)
        {
            FindMergingTerrain(pntr);

            if ((pntr.terrain != null) && (mergingTerrain != null))
                dest.Add(PainterDataAndConfig.terrainLight);
        }

        public override bool UpdateTylingFromMaterial(string fieldName, PlaytimePainter pntr)
        {
            if (pntr.terrain != null && fieldName != null)
            {
                if (fieldName.Contains(PainterDataAndConfig.terrainLight))
                {
                    var id = pntr.ImgData;
                    if (id != null)
                    {
                        id.tiling = Vector2.one;
                        id.offset = Vector2.zero;
                    }
                    return true; 
                }
            }
            return false;
        }

        public override bool setTextureOnMaterial(string fieldName, ImageData id, PlaytimePainter pntr)
        {
          //  if (id == null)
            //    return;

            Texture tex = id.CurrentTexture();

            if (pntr.terrain != null)
            {
                if (fieldName.Contains(PainterDataAndConfig.terrainLight))
                {
                    FindMergingTerrain(pntr);
                    if ((mergingTerrain != null) && (id!= null))
                        mergingTerrain.lightTexture = id.texture2D;

#if UNITY_EDITOR
                    if (tex is Texture2D)
                    {
                        UnityEditor.TextureImporter timp = ((Texture2D)tex).GetTextureImporter();
                        if (timp != null)
                        {
                            bool needReimport = timp.WasClamped();
                            needReimport |= timp.HadNoMipmaps();

                            if (needReimport)
                                timp.SaveAndReimport();
                        }
                    }
#endif

                    Shader.SetGlobalTexture(PainterDataAndConfig.terrainLight, tex);



                    return true;
                }
            }
            return false;
        }

        public override void OnUpdate(PlaytimePainter painter)
        {

            FindMergingTerrain(painter);

            if (mergingTerrain != null)
            {
                if (mergingTerrain.lightTexture != null)
                    Shader.SetGlobalTexture(PainterDataAndConfig.terrainLight, mergingTerrain.lightTexture.GetDestinationTexture());

                mergingTerrain.UpdateTextures();
            }

        }

    }

}
