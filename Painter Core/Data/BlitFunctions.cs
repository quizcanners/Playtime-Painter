using System;
using QuizCanners.Utils;
using UnityEngine;

namespace PlaytimePainter {

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration


    public static class BlitFunctions {

        public delegate void BlitModeFunction(ref Color dst);

        public static void Set(ColorMask mask) {

            r = mask.HasFlag(ColorMask.R);
            g = mask.HasFlag(ColorMask.G);
            b = mask.HasFlag(ColorMask.B);
            a = mask.HasFlag(ColorMask.A);

        }

        public static bool r;
        public static bool g;
        public static bool b;
        public static bool a;
        public static float alpha;

        public static int x;
        public static int y;
        public static int z;
        public static float half;
        public static float brAlpha;

        public static Func<bool> alphaMode;
        public static BlitModeFunction blitMode;

        public static Color cSrc;

        public static bool NoAlpha() => true;

        public static bool SphereAlpha()
        {
            var dist = 1 + half - Mathf.Sqrt(y * y + x * x + z * z);
            alpha = Mathf.Clamp01((dist) / half) * brAlpha;
            return alpha > 0;
        }

        public static bool CircleAlpha()
        {
            var dist = 1 + half - Mathf.Sqrt(y * y + x * x);
            alpha = Mathf.Clamp01((dist) / half) * brAlpha;
            return alpha > 0;
        }

        public static void AlphaBlitOpaque(ref Color cDst)
        {
            var deAlpha = 1 - alpha;

            if (r) cDst.r = Mathf.Sqrt(alpha * cSrc.r * cSrc.r + cDst.r * cDst.r * deAlpha);
            if (g) cDst.g = Mathf.Sqrt(alpha * cSrc.g * cSrc.g + cDst.g * cDst.g * deAlpha);
            if (b) cDst.b = Mathf.Sqrt(alpha * cSrc.b * cSrc.b + cDst.b * cDst.b * deAlpha);
            if (a) cDst.a = alpha * cSrc.a + cDst.a * deAlpha;
        }

        public static void AlphaBlitTransparent(ref Color cDst)
        {
            var rgbAlpha = cSrc.a * alpha;

            var divs = (cDst.a + rgbAlpha);
            
            rgbAlpha = divs > 0 ? Mathf.Clamp01(rgbAlpha / divs) : 0;

            var deAlpha = 1 - rgbAlpha;

            if (r) cDst.r = Mathf.Sqrt(rgbAlpha * cSrc.r * cSrc.r + cDst.r * cDst.r * deAlpha);
            if (g) cDst.g = Mathf.Sqrt(rgbAlpha * cSrc.g * cSrc.g + cDst.g * cDst.g * deAlpha);
            if (b) cDst.b = Mathf.Sqrt(rgbAlpha * cSrc.b * cSrc.b + cDst.b * cDst.b * deAlpha);
            if (a) cDst.a = alpha * cSrc.a + cDst.a * (1 - alpha);
        }
        
        public static void AddBlit(ref Color cDst)
        {
            if (r) cDst.r = alpha * cSrc.r + cDst.r;
            if (g) cDst.g = alpha * cSrc.g + cDst.g;
            if (b) cDst.b = alpha * cSrc.b + cDst.b;
            if (a) cDst.a = alpha * cSrc.a + cDst.a;
        }

        public static void SubtractBlit(ref Color cDst)
        {
            if (r) cDst.r = Mathf.Max(0, -alpha * cSrc.r + cDst.r);
            if (g) cDst.g = Mathf.Max(0, -alpha * cSrc.g + cDst.g);
            if (b) cDst.b = Mathf.Max(0, -alpha * cSrc.b + cDst.b);
            if (a) cDst.a = Mathf.Max(0, -alpha * cSrc.a + cDst.a);
        }

        public static void MaxBlit(ref Color cDst)
        {
            if (r) cDst.r += alpha * Mathf.Max(0, cSrc.r - cDst.r);
            if (g) cDst.g += alpha * Mathf.Max(0, cSrc.g - cDst.g);
            if (b) cDst.b += alpha * Mathf.Max(0, cSrc.b - cDst.b);
            if (a) cDst.a += alpha * Mathf.Max(0, cSrc.a - cDst.a);
        }

        public static void MinBlit(ref Color cDst)
        {
            if (r) cDst.r -= alpha * Mathf.Max(0, cDst.r - cSrc.r);
            if (g) cDst.g -= alpha * Mathf.Max(0, cDst.g - cSrc.g);
            if (b) cDst.b -= alpha * Mathf.Max(0, cDst.b - cSrc.b);
            if (a) cDst.a -= alpha * Mathf.Max(0, cDst.a - cSrc.a);
        }

        public static void PrepareCpuBlit(this Brush bc, TextureMeta id)
        {
            half = (bc.Size(false)) / 2;

            var smooth = bc.GetBrushType(true) != BrushTypes.Pixel.Inst;

            if (smooth)
                alphaMode = CircleAlpha;
            else
                alphaMode = NoAlpha;

            blitMode = bc.GetBlitMode(true).BlitFunctionTex2D(id);

            alpha = 1;

            Set(bc.mask);

            cSrc = bc.Color;

        }

        public static void Paint(Vector2 uvCoords, float brushAlpha, Texture2D texture, Vector2 offset, Vector2 tiling, Brush bc)
        {
            var id = texture.GetTextureMeta();

            id.offset = offset;
            id.tiling = tiling;

            var cmd = new PaintCommand.UV(new Stroke(uvCoords), texture.GetTextureMeta(), bc)
            {
                strokeAlphaPortion = brushAlpha
            };
            Paint(cmd);
        }

        public static void Paint(PaintCommand.UV command)
        {

            TextureMeta image = command.TextureData;
        
            if (image.Pixels == null)
                return;

            Brush bc = command.Brush;

            var uvCoords = command.Stroke.uvFrom;

            brAlpha = command.strokeAlphaPortion;

            bc.PrepareCpuBlit(image);

            Vector2 offset;

            var tmp = image.UvToPixelNumber(uvCoords, out offset);

            var smooth = bc.GetBrushType(true) != BrushTypes.Pixel.Inst;
            if (smooth) 
                offset = Vector2.zero;
            

            var hf = half - 0.5f;

            var halfFromX = Mathf.RoundToInt(-hf + offset.x);
            var halfFromY = Mathf.RoundToInt(-hf + offset.y);
            var halfToX = Mathf.RoundToInt(hf + offset.x);
            var halfToY = Mathf.RoundToInt(hf + offset.y);

            var fromX = tmp.x + halfFromX;

            tmp.y += halfFromY;

            var pixels = image.Pixels;

            for (y = halfFromY; y <= halfToY; y++) {

                tmp.x = fromX;

                for (x = halfFromX; x <= halfToX ; x++) {

                    if (alphaMode())
                        blitMode(ref pixels[image.PixelNo(tmp)]);

                    tmp.x += 1;
                }

                tmp.y += 1;
            }
            
        }
        
        #region Processors


        public static Color AddBackground(Color pix, Color colBG) {

            pix = pix * pix.a + colBG * (1 - pix.a);

            pix.a = 1;

            return pix;
        }

        public static Color ColorToAlpha(Color pix, Color colBG) {
            
            double pixR = pix.r;
            double pixG = pix.g;
            double pixB = pix.b;
            double pixA = pix.a;

            double colBgR = colBG.r;
            double colBgG = colBG.g;
            double colBgB = colBG.b;


            double a1 = 0;
            double a2 = 0;
            double a3 = 0;

            double diffR = pixR - colBgR;
            double diffG = pixG - colBgG;
            double diffB = pixB - colBgB;

            if (pixR > colBgR)
                a1 = diffR / (1d - colBgR);
            else if (pixR < colBgR)
                a1 = (colBgR - pixR) / colBgR;

            if (pixG > colBgG)
                a2 = diffG / (1d - colBgG);
            else if (pixG < colBgG)
                a2 = (colBgG - pixG) / colBgG;
 

            if (pixB > colBgB)
                a3 = diffB / (1d - colBgB);
            else if (pixB < colBgB)
                a3 = (colBgB - pixB) / colBgB;

            double aA = a1;
            if (a2 > aA) aA = a2;
            if (a3 > aA) aA = a3;

            if (aA >= 0.0001) {

                pixA = aA * pixA;

                double dA = 1f / aA;

                pixR = diffR * dA + colBgR;
                pixG = diffG * dA + colBgG;
                pixB = diffB * dA + colBgB;

                return new Color((float) pixR, (float)pixG, (float)pixB, (float)pixA);
            }

            return Color.clear;

        }

        #endregion
        
    }
}