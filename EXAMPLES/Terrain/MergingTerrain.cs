using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using Painter;





[ExecuteInEditMode]
public class MergingTerrain : MonoBehaviour {

    public List<channelSetsForCombinedMaps> mergeSubmasks;
    Color[] col;
    [HideInInspector]
    public PlaytimePainter painter;
    [HideInInspector]
    public Terrain terrain;
    public Texture2D lightTexture;

    public bool needToUpdateTextures;
    

   

    void OnEnable() {
        needToUpdateTextures = true;

        if (painter == null)
            painter = GetComponent<PlaytimePainter>();

        if (painter == null)
            painter = this.gameObject.AddComponent<PlaytimePainter>();
    }

    private void Update() {
        needToUpdateTextures |= mergeSubmasks.UpdateBumpGloss();

        if (needToUpdateTextures) {
            painter.UpdateShaderGlobalVariables();
            UpdateTextures();
        }

    }


    static ArrayManagerAbstract<SplatPrototype> splatArrayFuncs = new ArrayManagerAbstract<SplatPrototype>();


    public void UpdateTextures() {

        needToUpdateTextures = false;

        if (terrain == null) terrain = GetComponent<Terrain>();

        SplatPrototype[] copyProts = (terrain == null) ? null : terrain.GetCopyOfSplashPrototypes();

        if (mergeSubmasks != null) {

            int max = Mathf.Min(copyProts.Length, mergeSubmasks.Count);

            while ((mergeSubmasks.Count>max) && (mergeSubmasks[max].Product_colorWithAlpha != null) && (max<4)) {
                max++;
            }

            if (copyProts.Length < max) {
                int toAdd = max - copyProts.Length;
                splatArrayFuncs.Expand(ref copyProts, toAdd);
                for (int i= max - toAdd; i< max; i++) 
                    copyProts[i] = new SplatPrototype();
            }

            for (int i = 0; i < mergeSubmasks.Count; i++)
            {
                channelSetsForCombinedMaps tmp = mergeSubmasks[i];
                if (tmp.Product_combinedBump != null)
                    Shader.SetGlobalTexture(PainterConfig.terrainNormalMap + i, tmp.Product_combinedBump.getDestinationTexture());

                if (tmp.Product_colorWithAlpha != null)
                {
                    Shader.SetGlobalTexture(PainterConfig.terrainTexture + i, tmp.Product_colorWithAlpha.getDestinationTexture());
                    if ((copyProts != null) && (copyProts.Length > i))
                        copyProts[i].texture = tmp.Product_colorWithAlpha;
                }
            }
        }

        if (terrain != null)
            terrain.terrainData.splatPrototypes = copyProts;
     
    }



}

[Serializable]
public class TerrainLight : NonMaterialTexture {
    public MergingTerrain mergingTerrain;

 

    void findMergingTerrain(PlaytimePainter painter) {
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

    public override bool UpdateTyling(string fieldName, PlaytimePainter painter)
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
		Texture tex =  id.currentTexture();

        if (painter.terrain != null)
        {
            if (fieldName.Contains(PainterConfig.terrainLight)) {
                findMergingTerrain(painter);
                if (mergingTerrain!= null)
				    mergingTerrain.lightTexture = id.texture2D;

#if UNITY_EDITOR
                if (tex is Texture2D)    {
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

        if (mergingTerrain != null) {
            if (mergingTerrain.lightTexture != null)
                Shader.SetGlobalTexture(PainterConfig.terrainLight, mergingTerrain.lightTexture.getDestinationTexture());

            mergingTerrain.UpdateTextures();
        }

    }

}