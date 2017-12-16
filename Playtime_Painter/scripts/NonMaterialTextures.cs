using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

// This is a base class to use when managing special cases, such as: Terrain ControlMaps, Global Textures, Textures that are RAM only.

namespace Painter{

// Inherit this base class to create textures that are not not part of the material. (GlobalShaderValues, Terrain Textures)


public class NonMaterialTexture  {

    static List<Type> allTypes;

    public static void updateList(ref List<NonMaterialTexture> lst) {

        if (allTypes == null)
            allTypes = CsharpFuncs.GetAllChildTypesOf<NonMaterialTexture>();

        if (lst.Count == 0) {
            foreach (Type t in allTypes) 
                lst.Add((NonMaterialTexture)Activator.CreateInstance(t));
            return;
        }

        Debug.Log("lst was not null");

        foreach (Type t in allTypes) {
            if (!lst.ContainsInstanceType(t)) {
                Debug.Log("Creating instance of "+t.ToString());
                lst.Add((NonMaterialTexture)Activator.CreateInstance(t));
            }
        }

        for (int i = 0; i < lst.Count; i++)
            if (lst[i] == null) { lst.RemoveAt(i); i--; Debug.Log("Removing missing data"); }


}

    public  virtual bool getTexture(string fieldName, ref Texture tex, PlaytimePainter painter) {
        Debug.Log("Get Texture on " + this.GetType() + "  not implemented");
        return false;
    }

    public virtual void OnUpdate(PlaytimePainter painter) {

    }

    public virtual bool setTextureOnMaterial(string fieldName, imgData id, PlaytimePainter painter) {
        Debug.Log("Set Texture  on " + this.GetType() + "  not implemented");
        return false;
    }

    public virtual bool UpdateTyling(string fieldName, PlaytimePainter painter) {
        Debug.Log("Update Tiling on "+ this.GetType() +" not implemented");
        return false;
    }

    public virtual void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<string> dest) {
        Debug.Log("Get Names on " + this.GetType() + "  not implemented");
    }

}

public class TerrainControlGlob : NonMaterialTexture {

    public override bool getTexture(string fieldName, ref Texture tex, PlaytimePainter painter)
    {
        if ((painter.terrain != null) && (fieldName.Contains(painterConfig.terrainControl)))  {
            tex = painter.terrain.terrainData.alphamapTextures[fieldName[0].charToInt()];
            return true;
        }
        return false;
    }

    public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<string> dest)
    {
        if (painter.terrain != null) {
            Texture[] alphamaps = painter.terrain.terrainData.alphamapTextures;
            for (int i = 0; i < alphamaps.Length; i++)
                dest.Add(i + "_" + painterConfig.terrainControl);
        }
    }

    public override bool UpdateTyling(string fieldName, PlaytimePainter painter)
    {
        if (painter.terrain != null)
        {
            if (fieldName.Contains(painterConfig.terrainControl))
            {
                painter.curImgData.tyling = Vector2.one;
                painter.curImgData.offset = Vector2.zero;
                return true;
            }
        }
        return false;
      }

    public override bool setTextureOnMaterial(string fieldName, imgData id, PlaytimePainter painter)
    {
        Texture tex = id.currentTexture();
        if (painter.terrain != null)
        {
            if (fieldName.Contains(painterConfig.terrainControl))
            {
                int no = fieldName[0].charToInt();

                if (no == 0)
                    Shader.SetGlobalTexture(painterConfig.terrainControl, tex);

                painter.terrain.terrainData.alphamapTextures[no] = id.texture2D;

                return true;
            }
        }
        return false;
    }
}

public class TerrainSplatTexture : NonMaterialTexture
{
    public override bool getTexture(string fieldName, ref Texture tex, PlaytimePainter painter)
    {
        if ((painter.terrain != null) && (fieldName.Contains(painterConfig.terrainTexture)))
        {
            int no = fieldName[0].charToInt();
            tex = painter.terrain.terrainData.splatPrototypes[no].texture;




            return true;
        }
        return false;
    }

    public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<string> dest)
    {
        if (painter.terrain != null)
        {
            SplatPrototype[] sp = painter.terrain.terrainData.splatPrototypes;
            for (int i = 0; i < sp.Length; i++)
            {
                if (sp[i].texture != null)
                    dest.Add(i + painterConfig.terrainTexture + sp[i].texture.name);
            }
        }
    }

    public override bool UpdateTyling(string fieldName, PlaytimePainter painter) {

        if (painter.terrain != null) {
            if (fieldName.Contains(painterConfig.terrainTexture)) {
                int no = fieldName[0].charToInt();

                SplatPrototype[] splats = painter.terrain.terrainData.splatPrototypes;
                if (splats.Length <= no) return true; ;

                SplatPrototype sp = painter.terrain.terrainData.splatPrototypes[no];

                float width = painter.terrain.terrainData.size.x / sp.tileSize.x;
                float length = painter.terrain.terrainData.size.z / sp.tileSize.y;

                painter.curImgData.tyling = new Vector2(width, length);
                painter.curImgData.offset = sp.tileOffset;
                return true;
            }
        }
        return false;
    }

    public override bool setTextureOnMaterial(string fieldName, imgData id, PlaytimePainter painter)
    {
        Texture tex = id.currentTexture();
        if (painter.terrain != null)
        {
            if (fieldName.Contains(painterConfig.terrainTexture))
            {
                int no = fieldName[0].charToInt();
                painter.terrain.setSplashPrototypeTexture(id.texture2D, no);
                if (tex.GetType() != typeof(Texture2D))
                   
                //else
                    Debug.Log("Can only use Texture2D for Splat Prototypes. If using regular terrain may not see changes.");
                else
                    {

#if UNITY_EDITOR
                        UnityEditor.TextureImporter timp = ((Texture2D)tex).getTextureImporter();
                            if (timp != null)
                            {
                                bool needReimport = timp.wasClamped();
                                needReimport |= timp.hadNoMipmaps();

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

public class TerrainHeight : NonMaterialTexture
{

    public override bool getTexture(string fieldName, ref Texture tex, PlaytimePainter painter)
    {
        if ((painter.terrain != null) && (fieldName.Contains(painterConfig.terrainHeight)))
        {
            tex = painter.terrainHeightTexture;
            return true;
        }
        return false;
    }

    public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<string> dest) {
        if (painter.terrain != null)
            dest.Add(painterConfig.terrainHeight);
    }

    public override bool UpdateTyling(string fieldName, PlaytimePainter painter)
    {
        if (painter.terrain != null)
        {
            if (fieldName.Contains(painterConfig.terrainHeight))
            {
                painter.curImgData.tyling = Vector2.one;
                painter.curImgData.offset = Vector2.zero;
                return true;
            }
        }
        return false;
    }

    public override bool setTextureOnMaterial(string fieldName, imgData id, PlaytimePainter painter)
    {
        Texture tex = id.currentTexture();
        if (painter.terrain != null)
        {
            if (fieldName.Contains(painterConfig.terrainHeight))
            {
                    if (id != null)
                painter.terrainHeightTexture = id.texture2D;

                Shader.SetGlobalTexture(painterConfig.terrainHeight, tex);
                return true;
            }
        }
        return false;
    }

    public override void OnUpdate(PlaytimePainter painter) {
        if (painter.terrainHeightTexture != null)
            Shader.SetGlobalTexture(painterConfig.terrainHeight, painter.terrainHeightTexture.getDestinationTexture());
    }
}
}