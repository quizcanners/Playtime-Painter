using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public static class BumpGlossMerger {

#if UNITY_EDITOR

    static Color[] srcBmp;
    static Color[] srcSm;
    static Color[] srcAmbient;
    static Color[] dst;


    static int width;
    static int height;

    static int indexFrom(int x, int y) {

        x %= width;
        if (x < 0) x += width;
        y %= height;
        if (y < 0) y += height;

        return y * width + x;
    }

    public static Texture2D NormalMapFrom(float strength, float diagonalPixelsCoef, Texture2D bump, Texture2D normalReady, Texture2D smoothness, Texture2D ambient , string name, Texture2D Result) {

        if (bump == null) {
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
        BumpGlossMerger.height = bump.height;

        TextureImporter importer = bump.getTextureImporter();
        bool needReimport = importer.wasNotReadable();
        needReimport |= importer.wasNotSingleChanel();
        if (needReimport) importer.SaveAndReimport();

        if (normalReady != null) {
            importer = normalReady.getTextureImporter();
            needReimport = importer.wasNotReadable();
            needReimport |= importer.wasWrongDataType(false);
            needReimport |= importer.wasMarkedAsNormal();
            if (needReimport) importer.SaveAndReimport();
        }

        importer = smoothness.getTextureImporter();
        needReimport = importer.wasNotReadable();
        needReimport |= importer.wasWrongDataType(false);
        needReimport |= importer.wasNotSingleChanel();
        if (needReimport) importer.SaveAndReimport();

        importer = ambient.getTextureImporter();
        needReimport = importer.wasNotReadable();
        needReimport |= importer.wasWrongDataType(false);
        needReimport |= importer.wasNotSingleChanel();
        if (needReimport) importer.SaveAndReimport();

        try
        {

            srcBmp = (normalReady != null) ? normalReady.GetPixels(width, height) : bump.GetPixels();
            srcSm = smoothness.GetPixels(width, height);
            srcAmbient = ambient.GetPixels(width, height);
            dst = new Color[height * width];
        } catch (UnityException e) {
            Debug.Log("couldn't read one of the textures for  "+ bump.name +" " +e.ToString());
            return null;
        }


        for (int by = 0; by < BumpGlossMerger.height; by++)  {
            for (int bx = 0; bx < width; bx++) {

                int dstIndex = indexFrom(bx, by);

                if (normalReady)
                {
                    dst[dstIndex].r = (srcBmp[dstIndex].r-0.5f)* strength + 0.5f;
                    dst[dstIndex].g = (srcBmp[dstIndex].g-0.5f)* strength + 0.5f;
      
                }
                else
                {
                    //  float center = srcBmp[dstIndex].a;

                    xLeft = srcBmp[indexFrom(bx - 1, by)].a;
                    xRight = srcBmp[indexFrom(bx + 1, by)].a;
                    yUp = srcBmp[indexFrom(bx, by - 1)].a;
                    yDown = srcBmp[indexFrom(bx, by + 1)].a;

                    /*   float LUp = srcBmp[indexFrom(bx - 1, by - 1)].a ;
                       float RUp = srcBmp[indexFrom(bx + 1, by - 1)].a ;
                       float LDn = srcBmp[indexFrom(bx - 1, by + 1)].a ;
                       float RDn = srcBmp[indexFrom(bx + 1, by + 1)].a ;*/

                    //xDelta = Mathf.Clamp01(((   xLeft + (LUp + LDn) * diagonalPixelsCoef - xRight - (RUp + RDn) * diagonalPixelsCoef)*strength+ 0.5f) );
                    //  yDelta = Mathf.Clamp01(((yUp + (LUp + RUp) * diagonalPixelsCoef - yDown - (LDn + RDn) * diagonalPixelsCoef) * strength + 0.5f) );
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


       if ((Result == null) || (Result.width != width) || (Result.height != height))
            Result = bump.CreatePngSameDirectory(name + "_MASKnMAPS");

        TextureImporter resImp = Result.getTextureImporter();
        needReimport = resImp.wasClamped();
        needReimport |= resImp.wasWrongDataType(false);
        needReimport |= resImp.wasNotReadable();
        needReimport |= resImp.hadNoMipmaps();


        if (needReimport)
            resImp.SaveAndReimport();

        Result.SetPixels(dst);
        Result.Apply();
        Result.saveTexture();

        return Result;
    }


    public static Texture2D HeightToAlpha( Texture2D bump, Texture2D diffuse, string newName) {

        if (bump == null) {
            Debug.Log("No bump texture");
            return null;
        }

        TextureImporter ti = bump.getTextureImporter();
        bool needReimport = ti.wasNotSingleChanel();
        needReimport |= ti.wasNotReadable();
        if (needReimport)  ti.SaveAndReimport();


        ti = diffuse.getTextureImporter();
        needReimport = ti.wasAlphaNotTransparency();
        needReimport |= ti.wasNotReadable();
        if (needReimport) ti.SaveAndReimport();

        Texture2D product = diffuse.CreatePngSameDirectory(newName+"_COLOR");

        TextureImporter importer = product.getTextureImporter();
        needReimport = importer.wasNotReadable();
        needReimport |= importer.wasClamped();
        needReimport |= importer.hadNoMipmaps();
        if (needReimport)
            importer.SaveAndReimport();


        width = bump.width;
        BumpGlossMerger.height = bump.height;
        Color[] dstColor;

        try {
            dstColor = diffuse.GetPixels();
            srcBmp = bump.GetPixels(diffuse.width, diffuse.height);
        }
        catch (UnityException e) {
            Debug.Log("couldn't read one of the textures for  " + bump.name + " " + e.ToString());
            return null;
        }
        

        int dstIndex;
        for (int by = 0; by < BumpGlossMerger.height; by++)   {
            for (int bx = 0; bx < width; bx++)  {
                dstIndex = indexFrom(bx, by);
                Color col;
                col = dstColor[dstIndex];
                col.a = srcBmp[dstIndex].a;
                dstColor[dstIndex] = col;
            }
        }

        product.SetPixels(dstColor);
        product.Apply();
        product.saveTexture();

        return product;
    }
#endif

}
