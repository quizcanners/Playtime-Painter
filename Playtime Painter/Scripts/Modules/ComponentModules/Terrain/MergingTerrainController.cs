using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter
{

    [ExecuteInEditMode]
    public class MergingTerrainController : MonoBehaviour, IPEGI {

        [FormerlySerializedAs("mergeSubmasks")] public List<ChannelSetsForDefaultMaps> mergeSubMasks;
        [HideInInspector]
        public PlaytimePainter painter;
        [HideInInspector]
        public Terrain terrain;
        public Texture2D lightTexture;

        private void OnEnable()
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

            if (mergeSubMasks.IsNullOrEmpty()) return;
            
            var ls = terrain.terrainData.terrainLayers;

            var terrainLayersCount = ls?.Length ?? 0;
                
            for (var i = 0; i < mergeSubMasks.Count; i++) {

                var mergeSubMask = mergeSubMasks[i];

                if (mergeSubMask.Product_combinedBump)
                    Shader.SetGlobalTexture(PainterDataAndConfig.TERRAIN_NORMAL_MAP + i, mergeSubMask.Product_combinedBump.GetDestinationTexture());

                if (!mergeSubMask.Product_colorWithAlpha) continue;
                
                Shader.SetGlobalTexture(PainterDataAndConfig.TERRAIN_SPLAT_DIFFUSE + i, mergeSubMask.Product_colorWithAlpha.GetDestinationTexture());

                if (ls != null && (i < terrainLayersCount && ls[i]!= null))
                    ls[i].diffuseTexture = mergeSubMask.Product_colorWithAlpha;
            }

            if (terrain)
                terrain.terrainData.terrainLayers = ls;




        }

        #region Inspector
#if !NO_PEGI
        int inspectedElement = -1;
        public bool Inspect()
        {
            var changed = false;

            if ("Merge Sub Masks".edit_List(ref mergeSubMasks, ref inspectedElement).nl(ref changed)) {
                UpdateTextures();
                painter.UpdateShaderGlobals();
            }

            if (inspectedElement != -1) return changed;
            
            if (painter)
                if ("Height Texture".edit(70, ref painter.terrainHeightTexture).nl(ref changed))
                    painter.SetToDirty();

            changed |= "Light Texture ".edit(70, ref lightTexture).nl();


            if (changed || "Update".Click())
            {
                UpdateTextures();
                painter.UpdateShaderGlobals();
            }

            return changed;
        }


        public static bool PluginInspectPart() {

            const bool changed = false;

            var ptr = PlaytimePainter.inspected;

            if (!ptr || !ptr.terrain) return changed;
            
            var td = ptr.terrain.terrainData;

            if (td == null)
                "Terrain doesn't have terrain data".writeWarning();
            else
            {
                var layers = td.terrainLayers;
                if (layers == null)
                    "Terrain layers are null".writeWarning();
              
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

            private void RegenerateMasks()
            {

#if UNITY_EDITOR
                Product_combinedBump = NormalMapFrom(normalStrength, height, normalMap, ambient, productName, Product_combinedBump);
                if (colorTexture != null)
                    Product_colorWithAlpha = GlossToAlpha(smooth, colorTexture, productName);
#endif

            }

            #region Inspector
            public string NameForPEGI { get => productName; set => productName = value; }
#if !NO_PEGI

            public bool InspectInList(IList list, int ind, ref int edited)
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

        private static Color[] _srcBmp;
        private static Color[] _srcSm;
        private static Color[] _srcAmbient;
        private static Color[] _dst;

        private static int _width;
        private static int _height;

        private static int IndexFrom(int x, int y)
        {

            x %= _width;
            if (x < 0) x += _width;
            y %= _height;
            if (y < 0) y += _height;

            return y * _width + x;
        }

        private static Texture2D NormalMapFrom(float strength, Texture2D bump, Texture2D normalReady, Texture2D ambient, string name, Texture2D Result)
        {

            if (!bump)
            {
                Debug.Log("No bump texture");
                return null;
            }

            _width = bump.width;
            _height = bump.height;

            var importer = bump.GetTextureImporter();
            var needReimport = importer.WasNotReadable();
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
                _srcBmp = (normalReady != null) ? normalReady.GetPixels(_width, _height) : bump.GetPixels();
                _srcSm = bump.GetPixels(_width, _height);
                _srcAmbient = ambient.GetPixels(_width, _height);
                _dst = new Color[_height * _width];
            }
            catch (UnityException e)
            {
                Debug.Log("couldn't read one of the textures for  " + bump.name + " " + e.ToString());
                return null;
            }


            for (var by = 0; by < _height; by++)
            {
                for (var bx = 0; bx < _width; bx++)
                {

                    var dstIndex = IndexFrom(bx, by);

                    if (normalReady)
                    {
                        _dst[dstIndex].r = (_srcBmp[dstIndex].r - 0.5f) * strength + 0.5f;
                        _dst[dstIndex].g = (_srcBmp[dstIndex].g - 0.5f) * strength + 0.5f;

                    }
                    else
                    {

                        var xLeft = _srcBmp[IndexFrom(bx - 1, @by)].a;
                        var xRight = _srcBmp[IndexFrom(bx + 1, @by)].a;
                        var yUp = _srcBmp[IndexFrom(bx, @by - 1)].a;
                        var yDown = _srcBmp[IndexFrom(bx, @by + 1)].a;

                        var xDelta = (-xRight + xLeft) * strength;

                        var yDelta = (-yDown + yUp) * strength;

                        _dst[dstIndex].r = xDelta * Mathf.Abs(xDelta)
                            + 0.5f;
                        _dst[dstIndex].g = yDelta * Mathf.Abs(yDelta)
                            + 0.5f;
                    }

                    _dst[dstIndex].b = _srcSm[dstIndex].a;
                    _dst[dstIndex].a = _srcAmbient[dstIndex].a;
                }
            }


            if ((!Result) || (Result.width != _width) || (Result.height != _height))
                Result = bump.CreatePngSameDirectory(name + "_MASKnMAPS");

            var resImp = Result.GetTextureImporter();
            needReimport = resImp.WasClamped();
            needReimport |= resImp.WasWrongIsColor(false);
            needReimport |= resImp.WasNotReadable();
            needReimport |= resImp.HadNoMipmaps();


            if (needReimport)
                resImp.SaveAndReimport();

            Result.SetPixels(_dst);
            Result.Apply();
            Result.SaveTexture();

            return Result;
        }

        private static Texture2D GlossToAlpha(Texture2D gloss, Texture2D diffuse, string newName)
        {

            if (!gloss)
            {
                Debug.Log("No bump texture");
                return null;
            }

            var ti = gloss.GetTextureImporter();
            var needReimport = ti.WasNotSingleChanel();
            needReimport |= ti.WasNotReadable();
            if (needReimport) ti.SaveAndReimport();


            ti = diffuse.GetTextureImporter();
            needReimport = ti.WasAlphaNotTransparency();
            needReimport |= ti.WasNotReadable();
            if (needReimport) ti.SaveAndReimport();

            var product = diffuse.CreatePngSameDirectory(newName + "_COLOR");

            var importer = product.GetTextureImporter();
            needReimport = importer.WasNotReadable();
            needReimport |= importer.WasClamped();
            needReimport |= importer.HadNoMipmaps();
            if (needReimport)
                importer.SaveAndReimport();


            _width = gloss.width;
            _height = gloss.height;
            Color[] dstColor;

            try
            {
                dstColor = diffuse.GetPixels();
                _srcBmp = gloss.GetPixels(diffuse.width, diffuse.height);
            }
            catch (UnityException e)
            {
                Debug.Log("couldn't read one of the textures for  " + gloss.name + " " + e.ToString());
                return null;
            }


            for (var by = 0; by < _height; by++)
            {
                for (var bx = 0; bx < _width; bx++)
                {
                    var dstIndex = IndexFrom(bx, @by);
                    var col = dstColor[dstIndex];
                    col.a = _srcBmp[dstIndex].a;
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