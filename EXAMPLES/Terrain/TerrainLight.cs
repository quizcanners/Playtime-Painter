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

        void findMergingTerrain(PlaytimePainter painter)
        {
            if ((mergingTerrain == null) && (painter.terrain != null))
                mergingTerrain = painter.GetComponent<MergingTerrain>();
        }

        public override bool getTexture(string fieldName, ref Texture tex, PlaytimePainter painter)
        {
            if ((painter.terrain != null) && (fieldName.Contains(PainterConfig.terrainLight)))
            {
                tex = mergingTerrain.lightTexture;
                return true;
            }
            return false;
        }

        public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<string> dest)
        {
            findMergingTerrain(painter);

            if ((painter.terrain != null) && (mergingTerrain != null))
                dest.Add(PainterConfig.terrainLight);
        }

        public override bool UpdateTylingFromMaterial(string fieldName, PlaytimePainter painter)
        {
            if (painter.terrain != null)
            {
                if (fieldName.Contains(PainterConfig.terrainLight))
                {
                    painter.curImgData.tiling = Vector2.one;
                    painter.curImgData.offset = Vector2.zero;
                    return true; ;
                }
            }
            return false;
        }

        public override bool setTextureOnMaterial(string fieldName, imgData id, PlaytimePainter painter)
        {
          //  if (id == null)
            //    return;

            Texture tex = id.currentTexture();

            if (painter.terrain != null)
            {
                if (fieldName.Contains(PainterConfig.terrainLight))
                {
                    findMergingTerrain(painter);
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
