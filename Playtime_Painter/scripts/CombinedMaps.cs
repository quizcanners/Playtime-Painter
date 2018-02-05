using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using Painter;

public static class CombineMapsExtensions
{
    public static bool UpdateBumpGloss(this List<channelSetsForCombinedMaps> mergeSubmasks)
    {
        bool changes = false;

        if (mergeSubmasks != null)

            //for (int i = 0; i < mergeSubmasks.Length; i++) {
            foreach (var tmp in mergeSubmasks)
            {
                if (tmp.updateThis)
                {
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
public class channelSetsForCombinedMaps
{
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

    public channelSetsForCombinedMaps()
    {
        strength = 1;
    }


}
