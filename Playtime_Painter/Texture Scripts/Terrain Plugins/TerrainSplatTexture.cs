using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;


namespace Playtime_Painter
{

    [TaggedType(tag)]
    [System.Serializable]
    public class TerrainSplatTexture : PainterPluginBase
    {

        const string tag = "TerSplat";
        public override string ClassTag => tag;

        public override bool GetTexture(string fieldName, ref Texture tex, PlaytimePainter painter)
        {
            if ((painter.terrain != null) && (fieldName.Contains(PainterDataAndConfig.terrainTexture)))
            {
                int no = fieldName[0].CharToInt();



#if UNITY_2018_3_OR_NEWER
                var l = painter.terrain.terrainData.terrainLayers;

                if (l.Length > no)
                    tex = l[no].diffuseTexture;
#else

                tex = painter.terrain.terrainData.splatPrototypes[no].texture;
#endif
                return true;
            }
            return false;
        }

        public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<string> dest) {

            if (painter.terrain != null)  {

#if UNITY_2018_3_OR_NEWER
                var sp = painter.terrain.terrainData.terrainLayers;

                for (int i = 0; i < sp.Length; i++) {
                    var l = sp.TryGet(i);
                    if (l != null)
                        dest.Add(i + PainterDataAndConfig.terrainTexture + l.diffuseTexture.name);
                }

#else
                
                SplatPrototype[] sp = painter.terrain.terrainData.splatPrototypes;
                for (int i = 0; i < sp.Length; i++)
                {
                    if (sp[i].texture != null)
                        dest.Add(i + PainterDataAndConfig.terrainTexture + sp[i].texture.name);
                }
#endif
            }

        }

        public override bool UpdateTylingFromMaterial(string fieldName, PlaytimePainter painter)
        {

            if (painter.terrain != null)
            {
                if (fieldName.Contains(PainterDataAndConfig.terrainTexture))
                {
                    int no = fieldName[0].CharToInt();



#if UNITY_2018_3_OR_NEWER
                    var ls = painter.terrain.terrainData.terrainLayers;

        
                    if (ls.Length <= no) return true;

                    var l = ls.TryGet(no);

                    float width = painter.terrain.terrainData.size.x / l.tileSize.x;
                    float length = painter.terrain.terrainData.size.z / l.tileSize.y;

                    var id = painter.ImgData;
                    id.tiling = new Vector2(width, length);
                    id.offset = l.tileOffset;

#else
                    SplatPrototype[] splats = painter.terrain.terrainData.splatPrototypes;

                    if (splats.Length <= no) return true; 

                    SplatPrototype sp = painter.terrain.terrainData.splatPrototypes[no];

                        float width = painter.terrain.terrainData.size.x / sp.tileSize.x;
                    float length = painter.terrain.terrainData.size.z / sp.tileSize.y;

                    var id = painter.ImgData;
                    id.tiling = new Vector2(width, length);
                    id.offset = sp.tileOffset;

#endif


                    return true;
                }
            }
            return false;
        }

        public override bool SetTextureOnMaterial(string fieldName, ImageData id, PlaytimePainter painter)
        {
            Texture tex = id.CurrentTexture();
            if (painter.terrain != null)
            {
                if (fieldName.Contains(PainterDataAndConfig.terrainTexture))
                {
                    int no = fieldName[0].CharToInt();
                    painter.terrain.SetSplashPrototypeTexture(id.texture2D, no);
                    if (tex.GetType() != typeof(Texture2D))
                        Debug.Log("Can only use Texture2D for Splat Prototypes. If using regular terrain may not see changes.");
                    else
                    {

#if UNITY_EDITOR
                        UnityEditor.TextureImporter timp = ((Texture2D)tex).GetTextureImporter();
                        if (timp != null)
                        {
                            bool needReimport = timp.WasClamped();
                            needReimport |= timp.HadNoMipmaps();

                            if (needReimport)
                                timp.SaveAndReimport();
                        }
#endif

                    }
                    return true;
                }
            }
            return false;
        }
    }

}