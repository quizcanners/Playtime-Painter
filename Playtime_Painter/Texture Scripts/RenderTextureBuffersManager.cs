using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System.Collections.Generic;
using UnityEngine;

namespace Playtime_Painter {

    public static class RenderTextureBuffersManager {

        public static PainterDataAndConfig Data =>
            PainterCamera.Data;

        #region RenderTexture Buffers
        public static int renderBuffersSize = 2048;

        public static RenderTexture[] bigRtPair;
        public static RenderTexture alphaBufferTexture;

        public static void DiscardPaintingBuffersContents()
        {
            if (bigRtPair != null)
            foreach (var rt in bigRtPair)
                rt.DiscardContents();
        }

        public static void ClearAlphaBuffer()
        {
            alphaBufferTexture.DiscardContents();
            Blit(Color.clear, alphaBufferTexture);
        }

        public static int bigRtVersion;

        public static bool secondBufferUpdated;

        public static void UpdateBufferTwo()
        {
            //CurrentShader = PainterCamera.Data.pixPerfectCopy;
            bigRtPair[1].DiscardContents();
            Graphics.Blit(bigRtPair[0], bigRtPair[1]);
            secondBufferUpdated = true;
            bigRtVersion++;
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

            RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
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
                var sbf = new RenderTexture(width, width, 0, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Default);

                sbf.useMipMap = false;
                sbf.autoGenerateMips = false;

                _squareBuffers[no] = sbf;
            }

            return _squareBuffers[no];
        }
        #endregion

        #region Blit Textures

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

        public static void Blit(Texture tex, ImageMeta id)
        {
            if (!tex || id == null)
                return;
            var mat = TempMaterial(Data.pixPerfectCopy);
            var dst = id.CurrentRenderTexture();
            Graphics.Blit(tex, dst, mat);

            AfterBlit(dst);

        }

        public static void Blit(Texture from, RenderTexture to) => Blit(from, to, Data.pixPerfectCopy);

        public static void Blit(Texture from, RenderTexture to, Shader blitShader) {

            //if (!from)
              //  logger.Log_Interval(5, "Possibly Blitting null texture");
            
            var mat = TempMaterial(blitShader);
            Graphics.Blit(from, to, mat);
            AfterBlit(to);
        }

        static void AfterBlit(Texture target) {
            if (target && target == bigRtPair[0])
                secondBufferUpdated = false;
        }

        public static void Blit(Color col, RenderTexture to)
        {
            var tm = PainterCamera.Inst;

            var mat = tm.brushRenderer.Set(Data.bufferColorFill).Set(col).GetMaterial();

            Graphics.Blit(null, to, mat);
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
            if (depthTarget)
            {
                if (depthTarget.width == sizeOfDepthBuffers)
                    return;
                else
                    depthTarget.DestroyWhateverUnityObject();
            }

            var sz = Mathf.Max(sizeOfDepthBuffers, 16);

            depthTarget = GetDepthRenderTexture(sz);
            
        }

        #if PEGI
        public static bool InspectDepthTarget()
        {
            var changed = false;
            "Target Size".edit(ref sizeOfDepthBuffers).changes(ref changed);
            if (icon.Refresh.Click("Recreate Depth Texture").nl(ref changed))
            {
                depthTarget.DestroyWhatever();
                depthTarget = null;
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

        #endregion

        public static bool GotBuffers => !bigRtPair.IsNullOrEmpty();
        
        static void ClearBuffers()
        {
            foreach (var b in bigRtPair)
                b.DestroyWhatever();

            bigRtPair = null;

            alphaBufferTexture.DestroyWhatever();

        }

        public static void InitBrushBuffers()
        {
            if (!GotBuffers)
            {
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
                alphaBufferTexture = new RenderTexture(renderBuffersSize, renderBuffersSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Default);
                alphaBufferTexture.wrapMode = TextureWrapMode.Repeat;
                alphaBufferTexture.name = "Painting Alpha Buffer _ " + renderBuffersSize;
                alphaBufferTexture.useMipMap = false;
            }
        }
        
        #region Inspector

        static ChillLogger logger = new ChillLogger("error");
        
        #if PEGI
        private static int inspectedElement = -1;
        public static bool Inspect()
        {
            var changed = false;

            if (inspectedElement < 2 && "Refresh Buffers".Click().nl())
            {
                ClearBuffers();
                PainterCamera.Inst.RecreateBuffersIfDestroyed();
            }

            if ("Panting Buffers".enter(ref inspectedElement, 0).nl()) {
                
                if ("Buffer Size".selectPow2("Size of Buffers used for GPU painting", 90, ref renderBuffersSize, 64, 4096).nl()) {
                    PainterCamera.Inst.EmptyBufferTarget();
                    ClearBuffers();
                    PainterCamera.Inst.RecreateBuffersIfDestroyed();
                }

                bigRtPair[0].write(200);
                pegi.nl();
                bigRtPair[1].write(200);

                pegi.nl();
            }

            if ("Scaling Buffers".enter(ref inspectedElement, 1).nl())
            {

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
                "For Camera".edit(ref depthTarget).nl();
                depthTarget.write(250);

                "Reusable for blits".edit(ref _depthTargetForUsers).nl();
                _depthTargetForUsers.write(250);
            }

            if ("Render Textures with Depth buffer".enter(ref inspectedElement, 3).nl())
            {
                "Reusable".edit(ref renderTextureWithDepth).nl();
                renderTextureWithDepth.write(250);
            }

            return changed;
        }

        #endif
        #endregion

    }
}