using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using Painter;

public static class CombineMapsExtensions {
    public static bool UpdateBumpGloss(this List<channelSetsForCombinedMaps> mergeSubmasks) {
        bool changes = false;

        if (mergeSubmasks != null)

            //for (int i = 0; i < mergeSubmasks.Length; i++) {
            foreach (var tmp in mergeSubmasks) {
                if (tmp.updateThis) {
                    tmp.updateThis = false;
#if UNITY_EDITOR
                    tmp.Product_combinedBump = BumpGlossMerger.NormalMapFrom(tmp.strength, 0.1f, tmp.height, tmp.normalMap, tmp.smooth, tmp.ambient, tmp.productName, tmp.Product_combinedBump);
                    if (tmp.colorTexture != null)
                        tmp.Product_colorWithAlpha = BumpGlossMerger.HeightToAlpha(tmp.height, tmp.colorTexture, tmp.productName);
#endif
                    changes = true;
                }
            }
        return changes;
    }
}

[Serializable]
public class channelSetsForCombinedMaps {
    public string productName;
    public Texture2D colorTexture;
    public Texture2D height;
    public Texture2D normalMap;
    public Texture2D smooth;
    public Texture2D ambient;

    public Texture2D Product_colorWithAlpha;
    public Texture2D Product_combinedBump;
    public float strength;
    public bool updateThis;
    
    public channelSetsForCombinedMaps() {
        strength = 1;
    }

    
}

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
                    Shader.SetGlobalTexture(painterConfig.terrainNormalMap + i, tmp.Product_combinedBump.getDestinationTexture());

                if (tmp.Product_colorWithAlpha != null)
                {
                    Shader.SetGlobalTexture(painterConfig.terrainTexture + i, tmp.Product_colorWithAlpha.getDestinationTexture());
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
        if ((painter.terrain != null) && (fieldName.Contains(painterConfig.terrainLight)))
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
            dest.Add(painterConfig.terrainLight);
    }

    public override bool UpdateTyling(string fieldName, PlaytimePainter painter)
    {
        if (painter.terrain != null)
        {
            if (fieldName.Contains(painterConfig.terrainLight))
            {
                painter.curImgData.tyling = Vector2.one;
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
            if (fieldName.Contains(painterConfig.terrainLight)) {
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

                Shader.SetGlobalTexture(painterConfig.terrainLight, tex);



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
                Shader.SetGlobalTexture(painterConfig.terrainLight, mergingTerrain.lightTexture.getDestinationTexture());

            mergingTerrain.UpdateTextures();
        }

    }

}