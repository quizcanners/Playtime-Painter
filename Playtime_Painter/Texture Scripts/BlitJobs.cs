using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharedTools_Stuff;
using Unity.Jobs;
using Unity.Collections;

namespace Playtime_Painter
{

      public struct BlitJobs : IJob {
        
            public NativeArray<Color> values;
            bool r;
            bool g;
            bool b;
            bool a;

            int x;
            int y;
            int z;

            float alpha;

            float brAlpha;
            float half;
            bool smooth;
            int width;
            int height;
            MyIntVec2 pixelNumber;
            Color csrc;

        bool isVolumeBlit;
        int slices;
        int volHeight;
        int texWidth;
        Vector3 pos;

        Blit_Functions.alphaMode_dlg _alphaMode;
        Blit_Functions.blitModeFunction _blitMode;
        
        public void PrepareBlit(BrushConfig bc, ImageData id, float brushAlpha, StrokeVector stroke) {

            switch (blitJobBlitMode) {
                case BlitJobBlitMode.Add: _blitMode = AddBlit; break;
                case BlitJobBlitMode.Alpha:
                    if (bc.BlitMode.supportsTransparentLayer && id.isATransparentLayer)
                        _blitMode = AlphaBlitTransparent;
                    else
                        _blitMode = AlphaBlitOpaque; break;

                case BlitJobBlitMode.Max: _blitMode = MaxBlit; break;
                case BlitJobBlitMode.Min: _blitMode = MinBlit; break;
                case BlitJobBlitMode.Subtract: _blitMode = SubtractBlit; break;
                default: _blitMode = AlphaBlitOpaque; break;
            }
            
            if (smooth)
                _alphaMode = CircleAlpha;
            else
                _alphaMode = NoAlpha;

            values = id.pixelsForJob;
            pixelNumber = id.UvToPixelNumber(stroke.uvFrom);

            width = id.width;
            height = id.height;
            brAlpha = brushAlpha;

            half = (bc.Size(false)) / 2;
            smooth = bc.Type(true) != BrushTypePixel.Inst;

            blitJobBlitMode = bc.BlitMode.BlitJobFunction();

            alpha = 1;

            r = bc.mask.GetFlag(BrushMask.R);
            g = bc.mask.GetFlag(BrushMask.G);
            b = bc.mask.GetFlag(BrushMask.B);
            a = bc.mask.GetFlag(BrushMask.A);

            csrc = bc.colorLinear.ToGamma();
        }
        
        public void PrepareVolumeBlit(BrushConfig bc, ImageData id, float alpha, StrokeVector stroke, VolumeTexture volume) {
            PrepareBlit(bc, id, alpha, stroke);
            pos = (stroke.posFrom - volume.transform.position) / volume.size + 0.5f * Vector3.one;
            isVolumeBlit = true;
            slices = volume.h_slices;
            volHeight = volume.Height;
            texWidth = id.width;
        }

        int PixelNo(MyIntVec2 v)  {
            int x = v.x;
            int y = v.y;

            x %= width;
            if (x < 0)
                x += width;
            y %= height;
            if (y < 0)
                y += height;
            return y * width + x;
        }
        
        public void Execute() {

        

            if (!isVolumeBlit)
            {
                int ihalf = (int)(half - 0.5f);
                if (smooth) ihalf += 1;

                MyIntVec2 tmp = pixelNumber;

                int fromx = tmp.x - ihalf;

                tmp.y -= ihalf;

                for (y = -ihalf; y < ihalf + 1; y++)  {

                    tmp.x = fromx;

                    for (x = -ihalf; x < ihalf + 1; x++) {

                        if (_alphaMode()) {

                            var ind = PixelNo(tmp);
                            var col = values[ind];
                            _blitMode(ref col);
                            values[ind] = col;
                        }

                        tmp.x += 1;
                    }

                    tmp.y += 1;
                }

            } else {

                if (slices > 1)
                {
                    int ihalf = (int)(half - 0.5f);
                  
                    if (smooth) ihalf += 1;

                    _alphaMode = SphereAlpha;

                    int sliceWidth = texWidth / slices;

                    int hw = sliceWidth / 2;

                    y = (int)pos.y;
                    z = (int)(pos.z + hw);
                    x = (int)(pos.x + hw);
                    
                    for (y = -ihalf; y < ihalf + 1; y++)
                    {

                        int h = y + y;

                        if (h >= volHeight) return;

                        if (h >= 0)
                        {

                            int hy = h / slices;
                            int hx = h % slices;
                            int hTex_index = (hy * texWidth + hx) * sliceWidth;

                            for (z = -ihalf; z < ihalf + 1; z++) {

                                int trueZ = z + z;

                                if (trueZ >= 0 && trueZ < sliceWidth) {

                                    int yTex_index = hTex_index + trueZ * texWidth;

                                    for (x = -ihalf; x < ihalf + 1; x++) {
                                        int trueX = x + x;

                                        if (trueX >= 0 && trueX < sliceWidth) {

                                            int texIndex = yTex_index + trueX;

                                            if (_alphaMode())  {

                                                var col = values[texIndex];
                                                _blitMode(ref col);
                                                values[texIndex] =  col;
                                            }

                                        }
                                    }
                                }
                            }
                        }
                    }

                    return;
                }
                return;

            }
        }



        #region AlphaModes

         bool NoAlpha() => true;

        bool SphereAlpha() {
                float dist = 1 + half - Mathf.Sqrt(y * y + x * x + z * z);
                alpha = Mathf.Clamp01((dist) / half) * brAlpha;
                return alpha > 0;
            }

         bool CircleAlpha() {
                float dist = 1 + half - Mathf.Sqrt(y * y + x * x);
                alpha = Mathf.Clamp01((dist) / half) * brAlpha;
                return alpha > 0;
            }

        #endregion

        #region BlitModes

        public enum BlitJobBlitMode { Alpha, Add, Subtract, Max, Min }

        BlitJobBlitMode blitJobBlitMode;

      

         void AlphaBlitOpaque (ref Color cdst)
            {
                if (r) cdst.r = Mathf.Sqrt(alpha * csrc.r * csrc.r + cdst.r * cdst.r * (1.0f - alpha));
                if (g) cdst.g = Mathf.Sqrt(alpha * csrc.g * csrc.g + cdst.g * cdst.g * (1.0f - alpha));
                if (b) cdst.b = Mathf.Sqrt(alpha * csrc.b * csrc.b + cdst.b * cdst.b * (1.0f - alpha));
                if (a) cdst.a = alpha * csrc.a + cdst.a * (1.0f - alpha);
            }

        void AlphaBlitTransparent (ref Color cdst) {

            if (r) cdst.r = Mathf.Sqrt(alpha * csrc.r * csrc.r + cdst.r * cdst.r * (1.0f - alpha));
            if (g) cdst.g = Mathf.Sqrt(alpha * csrc.g * csrc.g + cdst.g * cdst.g * (1.0f - alpha));
            if (b) cdst.b = Mathf.Sqrt(alpha * csrc.b * csrc.b + cdst.b * cdst.b * (1.0f - alpha));
            if (a) cdst.a = alpha * csrc.a + cdst.a * (1.0f - alpha);
        }

        void AddBlit(ref Color cdst)
            {
                if (r) cdst.r = alpha * csrc.r + cdst.r;
                if (g) cdst.g = alpha * csrc.g + cdst.g;
                if (b) cdst.b = alpha * csrc.b + cdst.b;
                if (a) cdst.a = alpha * csrc.a + cdst.a;
            }

             void SubtractBlit(ref Color cdst)
            {
                if (r) cdst.r = Mathf.Max(0, -alpha * csrc.r + cdst.r);
                if (g) cdst.g = Mathf.Max(0, -alpha * csrc.g + cdst.g);
                if (b) cdst.b = Mathf.Max(0, -alpha * csrc.b + cdst.b);
                if (a) cdst.a = Mathf.Max(0, -alpha * csrc.a + cdst.a);
            }

             void MaxBlit(ref Color cdst)
            {
                if (r) cdst.r += alpha * Mathf.Max(0, csrc.r - cdst.r);
                if (g) cdst.g += alpha * Mathf.Max(0, csrc.g - cdst.g);
                if (b) cdst.b += alpha * Mathf.Max(0, csrc.b - cdst.b);
                if (a) cdst.a += alpha * Mathf.Max(0, csrc.a - cdst.a);
            }

             void MinBlit(ref Color cdst)
            {
                if (r) cdst.r -= alpha * Mathf.Max(0, cdst.r - csrc.r);
                if (g) cdst.g -= alpha * Mathf.Max(0, cdst.g - csrc.g);
                if (b) cdst.b -= alpha * Mathf.Max(0, cdst.b - csrc.b);
                if (a) cdst.a -= alpha * Mathf.Max(0, cdst.a - csrc.a);
            }


            #endregion




    }



}