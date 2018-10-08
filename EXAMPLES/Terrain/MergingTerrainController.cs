using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using PlayerAndEditorGUI;
using SharedTools_Stuff;
using Playtime_Painter.CombinedMaps;

namespace Playtime_Painter
{
    
    [ExecuteInEditMode]
    public class MergingTerrainController : MonoBehaviour, IPEGI
    {

        public List<ChannelSetsForDefaultMaps> mergeSubmasks;
        [HideInInspector]
        public PlaytimePainter painter;
        [HideInInspector]
        public Terrain terrain;
        public Texture2D lightTexture;

        public bool needToUpdateTextures;

        void OnEnable()
        {
            needToUpdateTextures = true;

            if (painter == null)
                painter = GetComponent<PlaytimePainter>();

            if (painter == null)
                painter = this.gameObject.AddComponent<PlaytimePainter>();
        }

        private void Update()
        {
            needToUpdateTextures |= mergeSubmasks.UpdateBumpGloss();

            if (needToUpdateTextures)
            {
                painter.UpdateShaderGlobals();
                UpdateTextures();
            }

        }


        static ArrayManagerAbstract<SplatPrototype> splatArrayFuncs = new ArrayManagerAbstract<SplatPrototype>();


        public void UpdateTextures()
        {

            needToUpdateTextures = false;

            if (terrain == null) terrain = GetComponent<Terrain>();

#if UNITY_2018_3_OR_NEWER
            var ls = (terrain) ? terrain.terrainData.terrainLayers : null ;

            if (ls == null)
            {
                Debug.Log("Terrain layers are null");
                return;
            }

            int copyProtsCount = ls.Length;

            if (mergeSubmasks != null)
            {

                int max = Mathf.Min(copyProtsCount, mergeSubmasks.Count);

                while ((mergeSubmasks.Count > max) && (mergeSubmasks[max].Product_colorWithAlpha != null) && (max < 4))
                    max++;

                for (int i = 0; i < Mathf.Max(mergeSubmasks.Count, ls.Length); i++)
                {

                    //if (i < mergeSubmasks.Count)
                    
                        ChannelSetsForDefaultMaps tmp = mergeSubmasks[i];
                        if (tmp.Product_combinedBump != null)
                            Shader.SetGlobalTexture(PainterDataAndConfig.terrainNormalMap + i, tmp.Product_combinedBump.GetDestinationTexture());
                    

                    if (tmp.Product_colorWithAlpha != null)
                    {
                        Shader.SetGlobalTexture(PainterDataAndConfig.terrainTexture + i, tmp.Product_colorWithAlpha.GetDestinationTexture());
                        if (i<ls.Length)
                            ls[i].diffuseTexture = tmp.Product_colorWithAlpha;

                        //if ((copyProts != null) && (copyProts.Length > i))
                        //     copyProts[i].texture = tmp.Product_colorWithAlpha;
                    }
                }

                if (terrain)
                    terrain.terrainData.terrainLayers = ls;
            }

#else
            SplatPrototype[] copyProts = (terrain) ? null : terrain.GetCopyOfSplashPrototypes();

            int copyProtsCount = copyProts == null ? 0 : copyProts.Length;
            
            if (mergeSubmasks != null)
            {

                int max = Mathf.Min(copyProtsCount, mergeSubmasks.Count);

                while ((mergeSubmasks.Count > max) && (mergeSubmasks[max].Product_colorWithAlpha != null) && (max < 4)) 
                    max++;
                

                if (copyProtsCount < max) {
                    int toAdd = max - copyProtsCount;

                    if (copyProts == null)
                        copyProts = new SplatPrototype[max];
                    else
                        splatArrayFuncs.Expand(ref copyProts, toAdd);

                    for (int i = max - toAdd; i < max; i++)
                        copyProts[i] = new SplatPrototype();
                }

                for (int i = 0; i < mergeSubmasks.Count; i++)  {
                    ChannelSetsForDefaultMaps tmp = mergeSubmasks[i];
                    if (tmp.Product_combinedBump != null)
                        Shader.SetGlobalTexture(PainterDataAndConfig.terrainNormalMap + i, tmp.Product_combinedBump.GetDestinationTexture());

                    if (tmp.Product_colorWithAlpha != null) {
                        Shader.SetGlobalTexture(PainterDataAndConfig.terrainTexture + i, tmp.Product_colorWithAlpha.GetDestinationTexture());
                        if ((copyProts != null) && (copyProts.Length > i))
                            copyProts[i].texture = tmp.Product_colorWithAlpha;
                    }
                }

                if (terrain)
                    terrain.terrainData.splatPrototypes = copyProts;
            }
#endif


        }
#if PEGI
        public bool Inspect() {
            bool changed = false;

            "Merge Submasks".edit(() => mergeSubmasks, this).nl();
            
            if (painter != null)
                changed |= "Light Texture ".edit(ref lightTexture).nl();

            if ("Update".Click()) 
                needToUpdateTextures = true;

            return changed;
        }

#endif
    }





}