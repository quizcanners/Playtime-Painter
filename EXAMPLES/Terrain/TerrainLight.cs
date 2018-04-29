using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace Playtime_Painter
{
    [System.Serializable]
    public class TerrainLight : PainterPluginBase
    {
    
        public MergingTerrain mergingTerrain;
        public int testData;

        public override bool BrushConfigPEGI()
        {
            bool changed = false;
           // if (pntr.terrain != null)
             //   "Found the terrain, yey!!".nl();
          //  changed |= "Test: ".edit(ref testData).nl();

            if (changed)
                pegi.SetToDirty(this);

            return changed;
        }

        void findMergingTerrain(PlaytimePainter pntr)
        {
            if ((mergingTerrain == null) && (pntr.terrain != null))
                mergingTerrain = pntr.GetComponent<MergingTerrain>();
        }

        public override bool getTexture(string fieldName, ref Texture tex, PlaytimePainter pntr)
        {
            if ((pntr.terrain != null) && (fieldName.Contains(PainterConfig.terrainLight)))
            {
                tex = mergingTerrain.lightTexture;
                return true;
            }
            return false;
        }

        public override void GetNonMaterialTextureNames(PlaytimePainter pntr, ref List<string> dest)
        {
            findMergingTerrain(pntr);

            if ((pntr.terrain != null) && (mergingTerrain != null))
                dest.Add(PainterConfig.terrainLight);
        }

        public override bool UpdateTylingFromMaterial(string fieldName, PlaytimePainter pntr)
        {
            if (pntr.terrain != null && fieldName != null)
            {
                if (fieldName.Contains(PainterConfig.terrainLight))
                {
                    var id = pntr.imgData;
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

            Texture tex = id.currentTexture();

            if (pntr.terrain != null)
            {
                if (fieldName.Contains(PainterConfig.terrainLight))
                {
                    findMergingTerrain(pntr);
                    if ((mergingTerrain != null) && (id!= null))
                        mergingTerrain.lightTexture = id.texture2D;

#if UNITY_EDITOR
                    if (tex is Texture2D)
                    {
                        UnityEditor.TextureImporter timp = ((Texture2D)tex).getTextureImporter();
                        if (timp != null)
                        {
                            bool needReimport = timp.wasClamped();
                            needReimport |= timp.hadNoMipmaps();

                            if (needReimport)
                                timp.SaveAndReimport();
                        }
                    }
#endif

                    Shader.SetGlobalTexture(PainterConfig.terrainLight, tex);



                    return true;
                }
            }
            return false;
        }

        public override void OnUpdate(PlaytimePainter painter)
        {

            findMergingTerrain(painter);

            if (mergingTerrain != null)
            {
                if (mergingTerrain.lightTexture != null)
                    Shader.SetGlobalTexture(PainterConfig.terrainLight, mergingTerrain.lightTexture.getDestinationTexture());

                mergingTerrain.UpdateTextures();
            }

        }

    }

}
