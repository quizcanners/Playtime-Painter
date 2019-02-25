using UnityEngine;
using QuizCannersUtilities;

namespace Playtime_Painter {

    public static class BlitFunctions {

        public delegate void BlitModeFunction(ref Color dst);
        public delegate bool PaintTexture2DMethod(StrokeVector stroke, float brushAlpha, ImageMeta image, BrushConfig bc, PlaytimePainter painter);

        public delegate bool AlphaModeDlg();

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

        public static AlphaModeDlg alphaMode;
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

        public static void PrepareCpuBlit(this BrushConfig bc, ImageMeta id)
        {
            half = (bc.Size(false)) / 2;

            var smooth = bc.Type(true) != BrushTypePixel.Inst;

            if (smooth)
                alphaMode = CircleAlpha;
            else
                alphaMode = NoAlpha;

            blitMode = bc.BlitMode.BlitFunctionTex2D(id);

            alpha = 1;

            var m = bc.mask;

            r = m.HasFlag(BrushMask.R);
            g = m.HasFlag(BrushMask.G);
            b = m.HasFlag(BrushMask.B);
            a = m.HasFlag(BrushMask.A);

            cSrc = bc.Color;

        }

        public static void Paint(Vector2 uvCoords, float brushAlpha, Texture2D texture, Vector2 offset, Vector2 tiling, BrushConfig bc, PlaytimePainter pntr)
        {
            var id = texture.GetImgData();

            id.offset = offset;
            id.tiling = tiling;

            Paint(new StrokeVector(uvCoords), brushAlpha, texture.GetImgData(), bc, pntr);
        }

        public static bool Paint(StrokeVector stroke, float brushAlpha, ImageMeta image, BrushConfig bc, PlaytimePainter painter)
        {

            if (image?.Pixels == null)
                return false;

            var uvCoords = stroke.uvFrom;

            brAlpha = brushAlpha;

            bc.PrepareCpuBlit(image);

            half -= 0.5f;

            var iHalf = (int)(half);

            Vector2 offset;

            var tmp = image.UvToPixelNumber(uvCoords, out offset);

            var smooth = bc.Type(true) != BrushTypePixel.Inst;
            if (smooth)
            {
                iHalf += 1;
                offset = Vector2.zero;
            }

            var halfFromX = Mathf.RoundToInt(-iHalf + offset.x);
            var halfFromY = Mathf.RoundToInt(-iHalf + offset.y);
            var halfToX = Mathf.RoundToInt(iHalf + offset.x);
            var halfToY = Mathf.RoundToInt(iHalf + offset.y);

            var fromX = tmp.x + halfFromX;

            tmp.y += halfFromY;

            var pixels = image.Pixels;

            for (y = halfFromY; y < halfToY + 1; y++) {

                tmp.x = fromX;

                for (x = halfFromX; x < halfToX + 1; x++) {

                    if (alphaMode())
                        blitMode(ref pixels[image.PixelNo(tmp)]);

                    tmp.x += 1;
                }

                tmp.y += 1;
            }

            return true;
        }


    }
}