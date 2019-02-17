using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Playtime_Painter
{

    [ExecuteInEditMode]
    public class MergingTerrainController : MonoBehaviour, IPEGI {

        public List<ChannelSetsForDefaultMaps> mergeSubmasks;
        [HideInInspector]
        public PlaytimePainter painter;
        [HideInInspector]
        public Terrain terrain;
        public Texture2D lightTexture;

        void OnEnable()
        {

            if (!painter)
                painter = GetComponent<PlaytimePainter>();

            if (!painter)
                painter = this.gameObject.AddComponent<PlaytimePainter>();

            UpdateTextures();
        }

        public void UpdateTextures()
        {
            if (!terrain)
                terrain = GetComponent<Terrain>();

            if (!terrain)
                return;
            
            if (!mergeSubmasks.IsNullOrEmpty())
            {
                var ls = terrain.terrainData.terrainLayers;

                int terrainLayersCount = ls!= null ? ls.Length : 0;
                
                for (int i = 0; i < mergeSubmasks.Count; i++) {

                    ChannelSetsForDefaultMaps mergeSubmask = mergeSubmasks[i];

                    if (mergeSubmask.Product_combinedBump)
                        Shader.SetGlobalTexture(PainterDataAndConfig.TERRAIN_NORMAL_MAP + i, mergeSubmask.Product_combinedBump.GetDestinationTexture());

                    if (mergeSubmask.Product_colorWithAlpha)
                    {
                        Shader.SetGlobalTexture(PainterDataAndConfig.TERRAIN_SPLAT_DIFFUSE + i, mergeSubmask.Product_colorWithAlpha.GetDestinationTexture());

                        if (i < terrainLayersCount && ls[i]!= null)
                            ls[i].diffuseTexture = mergeSubmask.Product_colorWithAlpha;
                    }
                }

                if (terrain)
                    terrain.terrainData.terrainLayers = ls;
            }




        }

        #region Inspector
#if PEGI
        int inspectedElement = -1;
        public bool Inspect()
        {
            bool changed = false;

            if ("Merge Submasks".edit_List(ref mergeSubmasks, ref inspectedElement).nl(ref changed)) {
                UpdateTextures();
                painter.UpdateShaderGlobals();
            }

            if (inspectedElement == -1) {

                if (painter)
                    if ("Height Texture".edit(70, ref painter.terrainHeightTexture).nl(ref changed))
                        painter.SetToDirty();

                changed |= "Light Texture ".edit(70, ref lightTexture).nl();


                if (changed || "Update".Click())
                {
                    UpdateTextures();
                    painter.UpdateShaderGlobals();
                }
            }

            return changed;
        }


        public bool PluginInspectPart() {

            var changed = false;

            var painter = PlaytimePainter.inspected;

            if (painter && painter.terrain)
            {


                var td = painter.terrain.terrainData;

                if (td == null)
                    "Terrain doesn't have terrain data".writeWarning();
                else
                {
                    var layers = td.terrainLayers;
                    if (layers == null)
                        "Terrain layers are null".writeWarning();
                    else
                    {
                       /* if (layers.Length< mergeSubmasks.Count) {

                            icon.Warning.write();

                            "Terrain has {0} layers while there are {1} Merge Submasks".F(layers.Length, mergeSubmasks.Count).write();
                            if (icon.Create.Click("Add layer").nl()) {
                                int index = layers.Length;
                                layers = layers.ExpandBy(1);
                                var l = new TerrainLayer();
                                l.diffuseTexture = mergeSubmasks[index].Product_colorWithAlpha;
                                layers[index] = l;
                                td.terrainLayers = layers;
                            }

                        }*/
                    }
                }
            }


            return changed;
        }


#endif
        #endregion

        [Serializable]
        public class ChannelSetsForDefaultMaps : IPEGI, IGotName, IPEGI_ListInspect
        {
            public string productName;
            public Texture2D colorTexture;
            public Texture2D height;
            public Texture2D normalMap;
            public Texture2D smooth;
            public Texture2D ambient;
            public Texture2D reflectiveness;

            public Texture2D Product_colorWithAlpha;
            public Texture2D Product_combinedBump;
            public int size = 1024;
            public float normalStrength = 1;

            void RegenerateMasks()
            {

#if UNITY_EDITOR
                Product_combinedBump = NormalMapFrom(normalStrength, 0.1f, height, normalMap, ambient, productName, Product_combinedBump);
                if (colorTexture != null)
                    Product_colorWithAlpha = GlossToAlpha(smooth, colorTexture, productName);
#endif

            }

            #region Inspector
            public string NameForPEGI { get => productName; set => productName = value; }
#if PEGI

            public bool PEGI_inList(IList list, int ind, ref int edited)
            {
                var changed = this.inspect_Name();

                if (!Product_colorWithAlpha)
                    "COl".edit(40, ref Product_colorWithAlpha).changes(ref changed);
                else
                    if (!Product_combinedBump)
                    "CMB".edit(40, ref Product_combinedBump).changes(ref changed);

                Product_colorWithAlpha.ClickHighlight();
                Product_combinedBump.ClickHighlight();

                if (icon.Enter.Click())
                    edited = ind;

                return changed;
            }

            public bool Inspect()
            {

                var changed = false;

                "Color".edit(90, ref colorTexture).nl();
                "Height".edit(90, ref height).nl();
                if (!normalMap && height)
                    "Normal from height strength".edit(ref normalStrength, 0, 1f).nl();
                "Bump".edit(90, ref normalMap).nl();

                "Smooth".edit(90, ref smooth).nl();
                "Ambient Occlusion".edit(90, ref ambient).nl();
                "Reflectivness".edit(110, ref reflectiveness).nl();

                "Size".edit(ref size).nl();

                if (size < 8)
                    "Size is too small".writeWarning();
                else
                if (!Mathf.IsPowerOfTwo(size))
                    "Size is not power of two".writeWarning();
                else if ("Generate".Click(ref changed))
                    RegenerateMasks();

                pegi.nl();

                "COLOR+GLOSS".edit(120, ref Product_colorWithAlpha).nl(ref changed);
                "BUMP+HEIGHT+AO".edit(120, ref Product_combinedBump).nl(ref changed);

                return changed;
            }
#endif
            #endregion

        }

#if UNITY_EDITOR

        static Color[] srcBmp;
        static Color[] srcSm;
        static Color[] srcAmbient;
        static Color[] dst;

        static int width;
        static int height;

        static int IndexFrom(int x, int y)
        {

            x %= width;
            if (x < 0) x += width;
            y %= height;
            if (y < 0) y += height;

            return y * width + x;
        }

        static Texture2D NormalMapFrom(float strength, float diagonalPixelsCoef, Texture2D bump, Texture2D normalReady, Texture2D ambient, string name, Texture2D Result)
        {

            if (!bump)
            {
                Debug.Log("No bump texture");
                return null;
            }

            float xLeft;
            float xRight;
            float yUp;
            float yDown;

            float yDelta;
            float xDelta;

            width = bump.width;
            height = bump.height;

            TextureImporter importer = bump.GetTextureImporter();
            bool needReimport = importer.WasNotReadable();
            needReimport |= importer.WasNotSingleChanel();
            if (needReimport) importer.SaveAndReimport();

            if (normalReady != null)
            {
                importer = normalReady.GetTextureImporter();
                needReimport = importer.WasNotReadable();
                needReimport |= importer.WasWrongIsColor(false);
                needReimport |= importer.WasMarkedAsNormal();
                if (needReimport) importer.SaveAndReimport();
            }

            importer = ambient.GetTextureImporter();
            needReimport = importer.WasNotReadable();
            needReimport |= importer.WasWrongIsColor(false);
            needReimport |= importer.WasNotSingleChanel();
            if (needReimport) importer.SaveAndReimport();

            try
            {
                srcBmp = (normalReady != null) ? normalReady.GetPixels(width, height) : bump.GetPixels();
                srcSm = bump.GetPixels(width, height);
                srcAmbient = ambient.GetPixels(width, height);
                dst = new Color[height * width];
            }
            catch (UnityException e)
            {
                Debug.Log("couldn't read one of the textures for  " + bump.name + " " + e.ToString());
                return null;
            }


            for (int by = 0; by < height; by++)
            {
                for (int bx = 0; bx < width; bx++)
                {

                    int dstIndex = IndexFrom(bx, by);

                    if (normalReady)
                    {
                        dst[dstIndex].r = (srcBmp[dstIndex].r - 0.5f) * strength + 0.5f;
                        dst[dstIndex].g = (srcBmp[dstIndex].g - 0.5f) * strength + 0.5f;

                    }
                    else
                    {

                        xLeft = srcBmp[IndexFrom(bx - 1, by)].a;
                        xRight = srcBmp[IndexFrom(bx + 1, by)].a;
                        yUp = srcBmp[IndexFrom(bx, by - 1)].a;
                        yDown = srcBmp[IndexFrom(bx, by + 1)].a;

                        xDelta = (-xRight + xLeft) * strength;

                        yDelta = (-yDown + yUp) * strength;

                        dst[dstIndex].r = xDelta * Mathf.Abs(xDelta)
                            + 0.5f;
                        dst[dstIndex].g = yDelta * Mathf.Abs(yDelta)
                            + 0.5f;
                    }

                    dst[dstIndex].b = srcSm[dstIndex].a;
                    dst[dstIndex].a = srcAmbient[dstIndex].a;
                }
            }


            if ((!Result) || (Result.width != width) || (Result.height != height))
                Result = bump.CreatePngSameDirectory(name + "_MASKnMAPS");

            TextureImporter resImp = Result.GetTextureImporter();
            needReimport = resImp.WasClamped();
            needReimport |= resImp.WasWrongIsColor(false);
            needReimport |= resImp.WasNotReadable();
            needReimport |= resImp.HadNoMipmaps();


            if (needReimport)
                resImp.SaveAndReimport();

            Result.SetPixels(dst);
            Result.Apply();
            Result.SaveTexture();

            return Result;
        }

        static Texture2D GlossToAlpha(Texture2D gloss, Texture2D diffuse, string newName)
        {

            if (!gloss)
            {
                Debug.Log("No bump texture");
                return null;
            }

            TextureImporter ti = gloss.GetTextureImporter();
            bool needReimport = ti.WasNotSingleChanel();
            needReimport |= ti.WasNotReadable();
            if (needReimport) ti.SaveAndReimport();


            ti = diffuse.GetTextureImporter();
            needReimport = ti.WasAlphaNotTransparency();
            needReimport |= ti.WasNotReadable();
            if (needReimport) ti.SaveAndReimport();

            Texture2D product = diffuse.CreatePngSameDirectory(newName + "_COLOR");

            TextureImporter importer = product.GetTextureImporter();
            needReimport = importer.WasNotReadable();
            needReimport |= importer.WasClamped();
            needReimport |= importer.HadNoMipmaps();
            if (needReimport)
                importer.SaveAndReimport();


            width = gloss.width;
            height = gloss.height;
            Color[] dstColor;

            try
            {
                dstColor = diffuse.GetPixels();
                srcBmp = gloss.GetPixels(diffuse.width, diffuse.height);
            }
            catch (UnityException e)
            {
                Debug.Log("couldn't read one of the textures for  " + gloss.name + " " + e.ToString());
                return null;
            }


            int dstIndex;
            for (int by = 0; by < height; by++)
            {
                for (int bx = 0; bx < width; bx++)
                {
                    dstIndex = IndexFrom(bx, by);
                    Color col;
                    col = dstColor[dstIndex];
                    col.a = srcBmp[dstIndex].a;
                    dstColor[dstIndex] = col;
                }
            }

            product.SetPixels(dstColor);
            product.Apply();
            product.SaveTexture();

            return product;
        }
#endif

    }

}