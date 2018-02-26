﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Painter{


public delegate void blitModeFunction(ref Color dst);

public static class Blit_Functions {

    public delegate bool alphaMode_dlg();

    static bool r;
    static bool g;
    static bool b;
    static bool a;
    static float alpha;

    static int x;
    static int y;
    static float half;
    static float brAlpha;

    static alphaMode_dlg _alphaMode;
    static blitModeFunction _blitMode;

    static Color csrc;

    static bool noAlpha() { return true; }

    static bool circleAlpha() {
        float dist = 1 + half - Mathf.Sqrt(y * y + x * x);
        alpha = Mathf.Clamp01((dist) / half) * brAlpha;
        return alpha > 0;
    }

    /*public static blitModeFunction blitFunction(this blitMode mode) {
        switch (mode) {
            case blitMode.AlphaBlit: return AlphaBlit;
            case blitMode.Add: return AddBlit;
            case blitMode.Max: return MaxBlit;
            case blitMode.Min: return MinBlit;
            default: return AlphaBlit;
        }
    }*/

		public static void AlphaBlit(ref Color cdst) {
			if (r) cdst.r = Mathf.Sqrt(alpha * csrc.r * csrc.r + cdst.r * cdst.r * (1.0f - alpha));
			if (g) cdst.g = Mathf.Sqrt(alpha * csrc.g * csrc.g + cdst.g * cdst.g * (1.0f - alpha));
			if (b) cdst.b = Mathf.Sqrt(alpha * csrc.b * csrc.b + cdst.b * cdst.b * (1.0f - alpha));
			if (a) cdst.a = alpha * csrc.a + cdst.a * (1.0f - alpha);
		}

		public static void AddBlit(ref Color cdst) {
        if (r) cdst.r = alpha * csrc.r + cdst.r;
        if (g) cdst.g = alpha * csrc.g + cdst.g;
        if (b) cdst.b = alpha * csrc.b + cdst.b;
        if (a) cdst.a = alpha * csrc.a + cdst.a;
    }

        public static void SubtractBlit(ref Color cdst) {
            if (r) cdst.r = Mathf.Max(0, -alpha * csrc.r + cdst.r);
            if (g) cdst.g = Mathf.Max(0, -alpha * csrc.g + cdst.g);
            if (b) cdst.b = Mathf.Max(0, -alpha * csrc.b + cdst.b);
            if (a) cdst.a = Mathf.Max(0, -alpha * csrc.a + cdst.a);
        }

		public static void MaxBlit(ref Color cdst)
    {
        if (r) cdst.r += alpha * Mathf.Max(0, csrc.r - cdst.r);
        if (g) cdst.g += alpha * Mathf.Max(0, csrc.g - cdst.g);
        if (b) cdst.b += alpha * Mathf.Max(0, csrc.b - cdst.b);
        if (a) cdst.a += alpha * Mathf.Max(0, csrc.a - cdst.a);
    }

		public static void MinBlit(ref Color cdst)
    {
        if (r) cdst.r -= alpha * Mathf.Max(0, cdst.r - csrc.r);
        if (g) cdst.g -= alpha * Mathf.Max(0, cdst.g - csrc.g);
        if (b) cdst.b -= alpha * Mathf.Max(0, cdst.b - csrc.b);
        if (a) cdst.a -= alpha * Mathf.Max(0, cdst.a - csrc.a);
    }


        public static void Paint(Vector2 uvCoords, float brushAlpha, Texture2D texture, Vector2 offset, Vector2 tiling, BrushConfig bc) {
            imgData id = texture.getImgData();

            id.offset = offset;
            id.tiling = tiling;

            if (id == null) {
#if UNITY_EDITOR
                Debug.Log("No texture data");
#endif
                return;
            }

            Paint(uvCoords, brushAlpha, texture.getImgData(), bc);
        }

        public static void Paint(Vector2 uvCoords, float brushAlpha, imgData image, BrushConfig bc) {

        brAlpha = brushAlpha;

        half = (bc.Size(false)) / 2;
        int ihalf = (int)(half-0.5f);

            bool smooth = bc.type != BrushTypePixel.inst;


        if (smooth)
            _alphaMode = circleAlpha;
        else
            _alphaMode = noAlpha;

			_blitMode = bc.blitMode.BlitFunctionTex2D;//bliTMode_Texture2D.blitFunction();

        if (smooth) ihalf += 1;

        alpha = 1;

         r = bc.mask.GetFlag(BrushMask.R);
         g = bc.mask.GetFlag(BrushMask.G);
         b = bc.mask.GetFlag(BrushMask.B);
         a = bc.mask.GetFlag(BrushMask.A);

        csrc = bc.color.ToColor();

		myIntVec2 tmp = image.uvToPixelNumber(uvCoords);//new myIntVec2 (pixIndex);

		int fromx = tmp.x - ihalf;

		tmp.y -= ihalf;
  
        for (y = -ihalf; y < ihalf + 1; y++) {
           
				tmp.x = fromx;

            for (x = -ihalf; x < ihalf + 1; x++) {
               
                if (_alphaMode())
						_blitMode(ref image.pixels[image.pixelNo(tmp)]);
                
					tmp.x += 1;
            }

				tmp.y += 1;
        }
    }

		public static void PaintAtlased(Vector2 uvCoords, float brushAlpha, 
			imgData image, BrushConfig bc, int atlasRows, Vector2 AtlasedSection) {

			imgData.sectorSize = image.width/atlasRows;
			imgData.atlasSector.From(AtlasedSection* imgData.sectorSize);

			brAlpha = brushAlpha;

			half = (bc.Size(false)) / 2;
			int ihalf = Mathf.FloorToInt(half-0.5f);

            bool smooth = bc.type != BrushTypePixel.inst;

			if (smooth)
				_alphaMode = circleAlpha;
			else
				_alphaMode = noAlpha;

			_blitMode = bc.blitMode.BlitFunctionTex2D;

			if (smooth) ihalf += 1;

			alpha = 1;

			r = bc.mask.GetFlag(BrushMask.R);
			g = bc.mask.GetFlag(BrushMask.G);
			b = bc.mask.GetFlag(BrushMask.B);
			a = bc.mask.GetFlag(BrushMask.A);

			csrc = bc.color.ToColor();

			myIntVec2 tmp = image.uvToPixelNumber(uvCoords);//new myIntVec2 (pixIndex);

			int fromx = tmp.x - ihalf;

			tmp.y -= ihalf;

		

			for (y = -ihalf; y < ihalf + 1; y++) {

				tmp.x = fromx;

				for (x = -ihalf; x < ihalf + 1; x++) {

					if (_alphaMode())
						_blitMode(ref image.pixels[image.pixelNoAtlased(tmp)]);

					tmp.x += 1;
				}

				tmp.y += 1;
			}
		}

}
}