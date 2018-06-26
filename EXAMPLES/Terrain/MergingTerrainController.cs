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
    public class MergingTerrainController : MonoBehaviour
#if PEGI
        , IPEGI
#endif
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

            SplatPrototype[] copyProts = (terrain) ? null : terrain.GetCopyOfSplashPrototypes();

            if (mergeSubmasks != null)
            {

                int max = Mathf.Min(copyProts.Length, mergeSubmasks.Count);

                while ((mergeSubmasks.Count > max) && (mergeSubmasks[max].Product_colorWithAlpha != null) && (max < 4)) {
                    max++;
                }

                if (copyProts.Length < max) {
                    int toAdd = max - copyProts.Length;
                    splatArrayFuncs.Expand(ref copyProts, toAdd);
                    for (int i = max - toAdd; i < max; i++)
                        copyProts[i] = new SplatPrototype();
                }

                for (int i = 0; i < mergeSubmasks.Count; i++)  {
                    ChannelSetsForDefaultMaps tmp = mergeSubmasks[i];
                    if (tmp.Product_combinedBump != null)
                        Shader.SetGlobalTexture(PainterConfig.terrainNormalMap + i, tmp.Product_combinedBump.getDestinationTexture());

                    if (tmp.Product_colorWithAlpha != null) {
                        Shader.SetGlobalTexture(PainterConfig.terrainTexture + i, tmp.Product_colorWithAlpha.getDestinationTexture());
                        if ((copyProts != null) && (copyProts.Length > i))
                            copyProts[i].texture = tmp.Product_colorWithAlpha;
                    }
                }
            }

            if (terrain != null)
                terrain.terrainData.splatPrototypes = copyProts;

        }
#if PEGI
        public bool PEGI() {
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