//#define debugInits

using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System.Collections.Generic;
using UnityEngine;

namespace Playtime_Painter {

    public static class RenderTextureBuffersManager {

        public static PainterDataAndConfig Data =>
            PainterCamera.Data;

        #region Tiny Texture

        public const int tinyTextureSize = 8;

        public static Texture2D GetMinSizeTexture()
        {
            if (tex)
                return tex;



            tex = new Texture2D(tinyTextureSize, tinyTextureSize, TextureFormat.RGBA32, false, true);



            return tex;
        }

        private static Texture2D tex;

        #endregion

        #region Painting Buffers
        public static int renderBuffersSize = 2048;

        private static RenderTexture[] bigRtPair;
        public static RenderTexture alphaBufferTexture;

        public static RenderTexture GetPaintingBufferIfExist(int index) => bigRtPair.TryGet(index);
                
        public static RenderTexture[] GetOrCreatePaintingBuffers ()
        {
            if (bigRtPair.IsNullOrEmpty() || !bigRtPair[0])
                InitBrushBuffers();

            return bigRtPair;
        }

        public static void DiscardPaintingBuffersContents()
        {
            if (bigRtPair != null)
            foreach (var rt in bigRtPair)
                rt.DiscardContents();
        }

        public static void ClearAlphaBuffer()
        {
            if (alphaBufferTexture) {
                alphaBufferTexture.DiscardContents();
                Blit(Color.clear, alphaBufferTexture);
            }
        }

        public static int bigRtVersion;

        public static bool secondBufferUpdated;

        public static void UpdateBufferTwo()
        {
            if (!bigRtPair.IsNullOrEmpty() && bigRtPair[0]) {
                bigRtPair[1].DiscardContents();
                Graphics.Blit(bigRtPair[0], bigRtPair[1]);
            } else 
            logger.Log_Interval(5, "Render Texture buffers are null");

            secondBufferUpdated = true;
            bigRtVersion++;
        }

        public static void InitBrushBuffers()
        {
            if (!GotPaintingBuffers)
            {

                #if debugInits
                Debug.Log("Creating Painting buffers");
                #endif


                bigRtPair = new RenderTexture[2];
                var tA = new RenderTexture(renderBuffersSize, renderBuffersSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
                var tB = new RenderTexture(renderBuffersSize, renderBuffersSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
                bigRtPair[0] = tA;
                bigRtPair[1] = tB;
                tA.useMipMap = false;
                tB.useMipMap = false;
                tA.wrapMode = TextureWrapMode.Repeat;
                tB.wrapMode = TextureWrapMode.Repeat;
                tA.name = "Painter Buffer 0 _ " + renderBuffersSize;
                tB.name = "Painter Buffer 1 _ " + renderBuffersSize;
            }

            if (!alphaBufferTexture)
            {
                alphaBufferTexture = new RenderTexture(renderBuffersSize, renderBuffersSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                alphaBufferTexture.wrapMode = TextureWrapMode.Repeat;
                alphaBufferTexture.name = "Painting Alpha Buffer _ " + renderBuffersSize;
                alphaBufferTexture.useMipMap = false;
            }
        }

        public static bool GotPaintingBuffers => !bigRtPair.IsNullOrEmpty();

        static void DestroyPaintingBuffers()
        {
            if (bigRtPair!= null)
            foreach (var b in bigRtPair)
                b.DestroyWhatever();

            bigRtPair = null;

            alphaBufferTexture.DestroyWhatever();

        }

        public static void RefreshPaintingBuffers()
        {
            PainterCamera.Inst.EmptyBufferTarget();
            DestroyPaintingBuffers();
            PainterCamera.Inst.RecreateBuffersIfDestroyed();
        }

        #endregion

        #region Scaling Buffers
        private const int squareBuffersCount = 13;
        
        private static readonly RenderTexture[] _squareBuffers = new RenderTexture[squareBuffersCount];

        static List<RenderTexture> nonSquareBuffers = new List<RenderTexture>();
        public static RenderTexture GetNonSquareBuffer(int width, int height)
        {
            foreach (RenderTexture r in nonSquareBuffers)
                if ((r.width == width) && (r.height == height)) return r;

            RenderTexture rt = new RenderTexture(width, height, 0, Data.useFloatForScalingBuffers ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.ARGB32
                , RenderTextureReadWrite.Default);
            nonSquareBuffers.Add(rt);
            return rt;
        }
        
        public static RenderTexture GetSquareBuffer(int width)
        {
            int no = squareBuffersCount - 1;
            switch (width)
            {
                case 1: no = 0; break;
                case 2: no = 1; break;
                case 4: no = 2; break;
                case 8: no = 3; break;
                case 16: no = 4; break;
                case 32: no = 5; break;
                case 64: no = 6; break;
                case 128: no = 7; break;
                case 256: no = 8; break;
                case 512: no = 9; break;
                case 1024: no = 10; break;
                case 2048: no = 11; break;
                case 4096: no = 12; break;
                default: logger.Log_Every(5, width + " is not in range "); break;
            }

            if (!_squareBuffers[no])
            {
                var sbf = new RenderTexture(width, width, 0, Data.useFloatForScalingBuffers ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.sRGB);

                sbf.useMipMap = false;
                sbf.autoGenerateMips = false;

                _squareBuffers[no] = sbf;


                #if debugInits
                Debug.Log("Creating Scaling buffer {0}*{0}".F(width));
                #endif

            }

            return _squareBuffers[no];
        }

        public static void DestroyScalingBuffers()
        {
            foreach (var tex in _squareBuffers)
                tex.DestroyWhateverUnityObject();

            foreach (var tex in nonSquareBuffers)
                tex.DestroyWhateverUnityObject();

            for (int i = 0; i < _squareBuffers.Length; i++)
                _squareBuffers[i] = null;

            nonSquareBuffers.Clear();
        }

        static RenderTexture SquareBuffer(int width) => GetSquareBuffer(width);

        public static RenderTexture GetDownscaledBigRt(int width, int height) => Downscale_ToBuffer(GetOrCreatePaintingBuffers()[0], width, height);
        
        public static RenderTexture GetDownscaleOf(Texture tex, int targetSize)
        {

            if (!tex)
                logger.Log_Interval(5, "Null texture as downscale source");
            else if (tex.width != tex.height)
                logger.Log_Interval(5, "Texture should be square");
            else if (!Mathf.IsPowerOfTwo(tex.width))
                logger.Log_Interval(5, "{0} is not a Power of two".F(tex));
            else
                return Downscale_ToBuffer(tex, targetSize, targetSize);


            return null;
        }

        public static RenderTexture Downscale_ToBuffer(Texture tex, int width, int height, Material material = null, Shader shader = null)
        {

            if (!tex)
                return null;

            bool usingCustom = material || shader;

            if (!shader)
                shader = Data.pixPerfectCopy;

            var cam = PainterCamera.Inst;

            bool square = (width == height);
            if (!square || !Mathf.IsPowerOfTwo(width))
            {
                return cam.Render(tex, GetNonSquareBuffer(width, height), shader);
                 


            }
            else
            {
                int tmpWidth = Mathf.Max(tex.width / 2, width);

                RenderTexture from = material
                    ? cam.Render(tex, SquareBuffer(tmpWidth), material)
                    : cam.Render(tex, SquareBuffer(tmpWidth), shader);


                while (tmpWidth > width)
                {

                    if (!usingCustom && tmpWidth / 4 > width)
                    {
                        tmpWidth /= 8;
                        from = cam.Render(from, SquareBuffer(tmpWidth), Data.bufferCopyDownscaleX8);
                        from.DiscardContents();

                    }
                    else if (!usingCustom && tmpWidth / 2 > width)
                    {

                        tmpWidth /= 4;
                        from = cam.Render(from, SquareBuffer(tmpWidth), Data.bufferCopyDownscaleX4);
                        from.DiscardContents();
                    }
                    else
                    {

                        tmpWidth /= 2;
                        from = material
                            ? cam.Render(from, SquareBuffer(tmpWidth), material)
                            : cam.Render(from, SquareBuffer(tmpWidth), shader);

                        from.DiscardContents();

                    }
                }

                return from;
            }
        }

        #endregion

        #region Depth Buffers 

        public static int sizeOfDepthBuffers = 512;

        public static RenderTexture depthTarget;
        private static RenderTexture _depthTargetForUsers;

        public static RenderTexture GetReusableDepthTarget()
        {
            if (_depthTargetForUsers)
                return _depthTargetForUsers;

            _depthTargetForUsers = GetDepthRenderTexture(1024);

            #if debugInits
            Debug.Log("Creating Reusable Depth Target {0}*{0}".F(1024));
            #endif

            return _depthTargetForUsers;
        }

        private static RenderTexture GetDepthRenderTexture(int sz) => new RenderTexture(sz, sz, 32, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            autoGenerateMips = false,
            useMipMap = false
        };

        public static void UpdateDepthTarget()
        {
            sizeOfDepthBuffers = Mathf.Max(sizeOfDepthBuffers, 16);

            if (depthTarget)
            {
                if (depthTarget.width == sizeOfDepthBuffers)
                    return;
                else
                    depthTarget.DestroyWhateverUnityObject();
            }

            depthTarget = GetDepthRenderTexture(sizeOfDepthBuffers);

            #if debugInits
            Debug.Log("Creating depth target {0}*{0}".F(sizeOfDepthBuffers));
            #endif

        }

        public static void DestroyDepthBuffers()
        {
            _depthTargetForUsers.DestroyWhateverUnityObject();
            _depthTargetForUsers = null;

            depthTarget.DestroyWhateverUnityObject();
            depthTarget = null;
        }

#if PEGI
        public static bool InspectDepthTarget()
        {
            var changed = false;
            "Target Size".edit(ref sizeOfDepthBuffers).changes(ref changed);
            if (icon.Refresh.Click("Recreate Depth Texture").nl(ref changed))
            {
                DestroyDepthBuffers();
                UpdateDepthTarget();
            }

            return changed;
        }
#endif



        #endregion

        #region RenderTexture with depth 

        private static RenderTexture renderTextureWithDepth;

        public static RenderTexture GetRenderTextureWithDepth()
        {
            if (!renderTextureWithDepth)
            {
                renderTextureWithDepth = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear);

                renderTextureWithDepth.useMipMap = false;
                renderTextureWithDepth.autoGenerateMips = false;

            }

            return renderTextureWithDepth;
        }

        public static void DestroyBuffersWithDepth()
        {
            renderTextureWithDepth.DestroyWhateverUnityObject();
            renderTextureWithDepth = null;
        }

        #endregion
        
        #region Blit Calls

        private static Material tmpMaterial;

        private static Material ReusableMaterial
        {
            get
            {
                if (!tmpMaterial)
                    tmpMaterial = new Material(PainterCamera.Data.pixPerfectCopy);

                return tmpMaterial;
            }
        }

        private static Material TempMaterial(Shader shade)
        {
            var mat = ReusableMaterial;
            mat.shader = shade;

            return mat;
        }

        public static RenderTexture Blit(Texture tex, ImageMeta id)
        {
            if (!tex || id == null)
                return null;
            var mat = TempMaterial(Data.pixPerfectCopy);
            var dst = id.CurrentRenderTexture();
            Graphics.Blit(tex, dst, mat);

            AfterBlit(dst);

            return dst;
        }

        public static RenderTexture Blit(Texture from, RenderTexture to) => Blit(from, to, Data.pixPerfectCopy);

        public static RenderTexture Blit(Texture from, RenderTexture to, Shader blitShader) => Blit(from, to, TempMaterial(blitShader));

        public static RenderTexture Blit(Texture from, RenderTexture to, Material blitMaterial) {
            Graphics.Blit(from, to, blitMaterial);
            AfterBlit(to);
            return to;
        }

        static void AfterBlit(Texture target) {
            if (target && target == bigRtPair[0])
                secondBufferUpdated = false;

            PainterCamera.sinceLastPainterCall = 0;
        }

        public static RenderTexture Blit(Color col, RenderTexture to)
        {
            var tm = PainterCamera.Inst;

            var mat = tm.brushRenderer.Set(Data.bufferColorFill).Set(col).GetMaterial();

            Graphics.Blit(null, to, mat);

            return to;
        }


        #endregion
        
        public static void OnDisable() {
            DestroyScalingBuffers();
            DestroyPaintingBuffers();
            DestroyDepthBuffers();
            DestroyBuffersWithDepth();
        }

        #region Inspector

        static ChillLogger logger = new ChillLogger("error");
        
        #if PEGI

        private static int inspectedElement = -1;

        public static bool Inspect() {
            var changed = false;

            if (inspectedElement < 2 && "Refresh Buffers".Click().nl())
                RefreshPaintingBuffers();

            if ("Panting Buffers".enter(ref inspectedElement, 0).nl()) {

                if (GotPaintingBuffers && icon.Delete.Click())
                    DestroyPaintingBuffers();

                if ("Buffer Size".selectPow2("Size of Buffers used for GPU painting", 90, ref renderBuffersSize, 64, 4096).nl()) 
                    RefreshPaintingBuffers();

                if (GotPaintingBuffers)
                {
                    bigRtPair[0].write(200);
                    pegi.nl();
                    bigRtPair[1].write(200);

                    pegi.nl();
                }

                "Depth".edit(ref alphaBufferTexture).nl();


            }

            if ("Scaling Buffers".enter(ref inspectedElement, 1).nl())
            {

                if (icon.Delete.Click())
                    DestroyScalingBuffers();

                if ("Use RGBAFloat for scaling".toggleIcon(ref Data.useFloatForScalingBuffers).nl(ref changed))
                    DestroyScalingBuffers();

                for (int i = 0; i < squareBuffersCount; i++) {

                    if (!_squareBuffers[i])
                        "No Buffer for {0}*{0}".F(Mathf.Pow(2, i)).nl();
                    else {

                        pegi.edit(ref _squareBuffers[i]).nl();
                        _squareBuffers[i].write(250);
                    }

                    pegi.nl();
                }
            }

            if ("Depth Texture".enter(ref inspectedElement, 2).nl()) {

                if (icon.Delete.Click())
                    DestroyDepthBuffers();

                "For Camera".edit(ref depthTarget).nl();
                depthTarget.write(250);
                pegi.nl();

                "Reusable for blits".edit(ref _depthTargetForUsers).nl();
                _depthTargetForUsers.write(250);
            }

            if ("Render Textures with Depth buffer".enter(ref inspectedElement, 3).nl())
            {
                if (icon.Delete.Click())
                    DestroyBuffersWithDepth();

                "Reusable".edit(ref renderTextureWithDepth).nl();
                renderTextureWithDepth.write(250);
            }

            if (changed)
                Data.SetToDirty();

            return changed;
        }

        #endif
        #endregion

    }
}