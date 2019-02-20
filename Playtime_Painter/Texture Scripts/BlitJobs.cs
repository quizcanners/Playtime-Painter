using UnityEngine;
using QuizCannersUtilities;
using Unity.Jobs;
using Unity.Collections;

namespace Playtime_Painter
{


    public enum BlitJobBlitMode { Alpha, Add, Subtract, Max, Min }

    public struct BlitJobs : IJob
    {
        private NativeArray<Color> _values;
        bool r;
        bool g;
        bool b;
        bool a;

        int x;
        int y;
        int z;

        private float _alpha;

        private float _brAlpha;
        private float _half;
        private bool _smooth;
        private int _width;
        private int _height;
        private MyIntVec2 _pixelNumber;
        private Color _srcCol;

        private bool _isVolumeBlit;
        private int _slices;
        private int _volHeight;
        private int _texWidth;
        private Vector3 _pos;

        private BlitFunctions.AlphaModeDlg _alphaMode;
        private BlitFunctions.BlitModeFunction _blitMode;

        private void PrepareBlit(BrushConfig bc, ImageMeta id, float brushAlpha, StrokeVector stroke)
        {

            switch (_blitJobBlitMode)
            {
                case BlitJobBlitMode.Add: _blitMode = AddBlit; break;
                case BlitJobBlitMode.Alpha:
                    if (bc.BlitMode.SupportsTransparentLayer && id.isATransparentLayer)
                        _blitMode = AlphaBlitTransparent;
                    else
                        _blitMode = AlphaBlitOpaque; break;

                case BlitJobBlitMode.Max: _blitMode = MaxBlit; break;
                case BlitJobBlitMode.Min: _blitMode = MinBlit; break;
                case BlitJobBlitMode.Subtract: _blitMode = SubtractBlit; break;
                default: _blitMode = AlphaBlitOpaque; break;
            }

            if (_smooth)
                _alphaMode = CircleAlpha;
            else
                _alphaMode = NoAlpha;

            _values = id.pixelsForJob;
            _pixelNumber = id.UvToPixelNumber(stroke.uvFrom);

            _width = id.width;
            _height = id.height;
            _brAlpha = brushAlpha;

            _half = (bc.Size(false)) / 2;
            _smooth = bc.Type(true) != BrushTypePixel.Inst;

            _blitJobBlitMode = bc.BlitMode.BlitJobFunction();

            _alpha = 1;

            r = BrushExtensions.HasFlag(bc.mask, BrushMask.R);
            g = BrushExtensions.HasFlag(bc.mask, BrushMask.G);
            b = BrushExtensions.HasFlag(bc.mask, BrushMask.B);
            a = BrushExtensions.HasFlag(bc.mask, BrushMask.A);

            _srcCol = bc.Color;
        }

        public void PrepareVolumeBlit(BrushConfig bc, ImageMeta id, float alpha, StrokeVector stroke, VolumeTexture volume)
        {
            PrepareBlit(bc, id, alpha, stroke);
            _pos = (stroke.posFrom - volume.transform.position) / volume.size + 0.5f * Vector3.one;
            _isVolumeBlit = true;
            _slices = volume.hSlices;
            _volHeight = volume.Height;
            _texWidth = id.width;
        }

        private int PixelNo(MyIntVec2 v)
        {
            var x = v.x;
            var y = v.y;

            x %= _width;
            if (x < 0)
                x += _width;
            y %= _height;
            if (y < 0)
                y += _height;
            return y * _width + x;
        }

        public void Execute()
        {



            if (!_isVolumeBlit)
            {
                var iHalf = (int)(_half - 0.5f);
                if (_smooth) iHalf += 1;

                var tmp = _pixelNumber;

                var fromX = tmp.x - iHalf;

                tmp.y -= iHalf;

                for (y = -iHalf; y < iHalf + 1; y++)
                {

                    tmp.x = fromX;

                    for (x = -iHalf; x < iHalf + 1; x++)
                    {

                        if (_alphaMode())
                        {

                            var ind = PixelNo(tmp);
                            var col = _values[ind];
                            _blitMode(ref col);
                            _values[ind] = col;
                        }

                        tmp.x += 1;
                    }

                    tmp.y += 1;
                }

            }
            else
            {
                if (_slices <= 1) return;
                
                var iHalf = (int)(_half - 0.5f);

                if (_smooth) iHalf += 1;

                _alphaMode = SphereAlpha;

                var sliceWidth = _texWidth / _slices;

                var hw = sliceWidth / 2;

                y = (int)_pos.y;
                z = (int)(_pos.z + hw);
                x = (int)(_pos.x + hw);

                for (y = -iHalf; y < iHalf + 1; y++)
                {

                    var h = y + y;

                    if (h >= _volHeight) return;

                    if (h < 0) continue;
                    
                    var hy = h / _slices;
                    var hx = h % _slices;
                    var hTexIndex = (hy * _texWidth + hx) * sliceWidth;

                    for (z = -iHalf; z < iHalf + 1; z++)
                    {

                        var trueZ = z + z;

                        if (trueZ < 0 || trueZ >= sliceWidth) continue;
                        
                        var yTexIndex = hTexIndex + trueZ * _texWidth;

                        for (x = -iHalf; x < iHalf + 1; x++)
                        {
                            var trueX = x + x;

                            if (trueX < 0 || trueX >= sliceWidth) continue;
                            var texIndex = yTexIndex + trueX;

                            if (!_alphaMode()) continue;
                            var col = _values[texIndex];
                            _blitMode(ref col);
                            _values[texIndex] = col;
                        }
                    }
                }
            }
        }

        #region AlphaModes

        private static bool NoAlpha() => true;

        private bool SphereAlpha()
        {
            var dist = 1 + _half - Mathf.Sqrt(y * y + x * x + z * z);
            _alpha = Mathf.Clamp01((dist) / _half) * _brAlpha;
            return _alpha > 0;
        }

        private bool CircleAlpha()
        {
            var dist = 1 + _half - Mathf.Sqrt(y * y + x * x);
            _alpha = Mathf.Clamp01((dist) / _half) * _brAlpha;
            return _alpha > 0;
        }

        #endregion

        #region BlitModes

        private BlitJobBlitMode _blitJobBlitMode;


        private void AlphaBlitOpaque(ref Color colDst)
        {
            var deAlpha = 1 - _alpha;

            if (r) colDst.r = Mathf.Sqrt(_alpha * _srcCol.r * _srcCol.r + colDst.r * colDst.r * deAlpha);
            if (g) colDst.g = Mathf.Sqrt(_alpha * _srcCol.g * _srcCol.g + colDst.g * colDst.g * deAlpha);
            if (b) colDst.b = Mathf.Sqrt(_alpha * _srcCol.b * _srcCol.b + colDst.b * colDst.b * deAlpha);
            if (a) colDst.a = _alpha * _srcCol.a + colDst.a * deAlpha;
        }

        private void AlphaBlitTransparent(ref Color colDst)
        {

            var rgbAlpha = _srcCol.a * _alpha;

            var divs = (colDst.a + rgbAlpha);


            rgbAlpha = divs > 0 ? Mathf.Clamp01(rgbAlpha / divs) : 0;
            
            var deRgbAlpha = 1 - rgbAlpha;

            if (r) colDst.r = Mathf.Sqrt(rgbAlpha * _srcCol.r * _srcCol.r + colDst.r * colDst.r * deRgbAlpha);
            if (g) colDst.g = Mathf.Sqrt(rgbAlpha * _srcCol.g * _srcCol.g + colDst.g * colDst.g * deRgbAlpha);
            if (b) colDst.b = Mathf.Sqrt(rgbAlpha * _srcCol.b * _srcCol.b + colDst.b * colDst.b * deRgbAlpha);
            if (a) colDst.a = _alpha * _srcCol.a + colDst.a * (1 - _alpha);
        }

        private void AddBlit(ref Color colDst)
        {
            if (r) colDst.r = _alpha * _srcCol.r + colDst.r;
            if (g) colDst.g = _alpha * _srcCol.g + colDst.g;
            if (b) colDst.b = _alpha * _srcCol.b + colDst.b;
            if (a) colDst.a = _alpha * _srcCol.a + colDst.a;
        }

        private void SubtractBlit(ref Color colDst)
        {
            if (r) colDst.r = Mathf.Max(0, -_alpha * _srcCol.r + colDst.r);
            if (g) colDst.g = Mathf.Max(0, -_alpha * _srcCol.g + colDst.g);
            if (b) colDst.b = Mathf.Max(0, -_alpha * _srcCol.b + colDst.b);
            if (a) colDst.a = Mathf.Max(0, -_alpha * _srcCol.a + colDst.a);
        }

        private void MaxBlit(ref Color colDst)
        {
            if (r) colDst.r += _alpha * Mathf.Max(0, _srcCol.r - colDst.r);
            if (g) colDst.g += _alpha * Mathf.Max(0, _srcCol.g - colDst.g);
            if (b) colDst.b += _alpha * Mathf.Max(0, _srcCol.b - colDst.b);
            if (a) colDst.a += _alpha * Mathf.Max(0, _srcCol.a - colDst.a);
        }

        private void MinBlit(ref Color colDst)
        {
            if (r) colDst.r -= _alpha * Mathf.Max(0, colDst.r - _srcCol.r);
            if (g) colDst.g -= _alpha * Mathf.Max(0, colDst.g - _srcCol.g);
            if (b) colDst.b -= _alpha * Mathf.Max(0, colDst.b - _srcCol.b);
            if (a) colDst.a -= _alpha * Mathf.Max(0, colDst.a - _srcCol.a);
        }


        #endregion

    }


}
