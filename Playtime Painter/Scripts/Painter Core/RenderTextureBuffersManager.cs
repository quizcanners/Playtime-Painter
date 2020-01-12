//#define debugInits

using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;
using PlaytimePainter.MeshEditing;

namespace PlaytimePainter {

#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable IDE0019 // Use pattern matching
#pragma warning disable IDE0018 // Inline variable declaration


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
                alphaBufferTexture = new RenderTexture(renderBuffersSize, renderBuffersSize, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
                {
                    wrapMode = TextureWrapMode.Repeat,
                    name = "Painting Alpha Buffer _ " + renderBuffersSize,
                    useMipMap = false
                };
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
            GetOrCreatePaintingBuffers();
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
                    RenderTextureReadWrite.sRGB)
                {
                    useMipMap = false,
                    autoGenerateMips = false
                };

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

        public static RenderTexture GetDownscaledBigRt(int width, int height, bool allowApprox = false) => Downscale_ToBuffer(GetOrCreatePaintingBuffers()[0], width, height, allowApprox: allowApprox);
        
        public static RenderTexture GetDownscaleOf(Texture tex, int targetSize, bool allowApprox = false)
        {

            if (!tex)
                logger.Log_Interval(5, "Null texture as downscale source");
            else if (tex.width != tex.height)
                logger.Log_Interval(5, "Texture should be square");
            else if (!Mathf.IsPowerOfTwo(tex.width))
                logger.Log_Interval(5, "{0} is not a Power of two".F(tex));
            else
                return Downscale_ToBuffer(tex, targetSize, targetSize, allowApprox: allowApprox);


            return null;
        }

        public static RenderTexture Downscale_ToBuffer(Texture tex, int width, int height, Material material = null, Shader shader = null, bool allowApprox = false)
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
                
                while (tmpWidth > width) {

                    bool jobDone = false;

                    var previousFrom = from;

                    if (!usingCustom) {

                        if (allowApprox) {

                            if (tmpWidth / 32 > width) {

                                tmpWidth /= 64;
                                from = cam.Render(from, SquareBuffer(tmpWidth), Data.bufferCopyDownscaleX64_Approx);
                                jobDone = true;

                            } else if (tmpWidth / 16 > width) {

                                tmpWidth /= 32;
                                from = cam.Render(from, SquareBuffer(tmpWidth), Data.bufferCopyDownscaleX32_Approx);
                                jobDone = true;

                            }
                            else if (tmpWidth / 8 > width) {

                                tmpWidth /= 16;
                                from = cam.Render(from, SquareBuffer(tmpWidth), Data.bufferCopyDownscaleX16_Approx);
                                jobDone = true;

                            }
                        }

                        if (!jobDone) {
                            if (tmpWidth / 4 > width)
                            {
                                tmpWidth /= 8;
                                from = cam.Render(from, SquareBuffer(tmpWidth), Data.bufferCopyDownscaleX8);
                                jobDone = true;
                            }
                            else if (tmpWidth / 2 > width)
                            {
                                tmpWidth /= 4;
                                from = cam.Render(from, SquareBuffer(tmpWidth), Data.bufferCopyDownscaleX4);
                                jobDone = true;
                            }
                        }
                    }

                    if (!jobDone) {
                        tmpWidth /= 2;
                        from = material
                            ? cam.Render(from, SquareBuffer(tmpWidth), material)
                            : cam.Render(from, SquareBuffer(tmpWidth), shader);
                    }

                    previousFrom.DiscardContents();
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



        #endregion

        #region RenderTexture with depth 

        private static RenderTexture renderTextureWithDepth;

        public static RenderTexture GetRenderTextureWithDepth()
        {
            if (!renderTextureWithDepth)
            {
                renderTextureWithDepth = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear)
                {
                    useMipMap = false,
                    autoGenerateMips = false
                };

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

        public static RenderTexture Blit(Texture tex, TextureMeta id)
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
            if (target && target.IsBigRenderTexturePair())
                secondBufferUpdated = false;

            PainterCamera.lastPainterCall = Time.time;
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

        static QcUtils.ChillLogger logger = new QcUtils.ChillLogger("error");
        
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

                if (icon.Delete.Click().nl())
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
        
        #endregion

    }



    public static class TextureEditorExtensionFunctions
    {

        public static void TeachingNotification(this string text)
        {
            if (PainterCamera.Data && PainterCamera.Data.showTeachingNotifications)
                text.showNotificationIn3D_Views();
        }

        public static Mesh GetMesh(this PlaytimePainter p)
        {
            if (!p) return null;

            if (p.skinnedMeshRenderer)
                return p.colliderForSkinnedMesh;

            return p.SharedMesh;

        }

        public static float StrokeWidth(this BrushConfig br, float pixWidth, bool world) => br.Size(world) / (pixWidth) * 2 * PainterCamera.OrthographicSize;

        public static bool IsSingleBufferBrush(this BrushConfig b) => (PainterCamera.Inst.IsLinearColorSpace && b.GetBlitMode(false).SupportedBySingleBuffer && b.GetBrushType(false).SupportedBySingleBuffer && b.PaintingRGB);

        public static bool IsProjected(this Material mat) => mat && mat.shaderKeywords.Contains(PainterShaderVariables.UV_PROJECTED);

        public static bool NeedsGrid(this PlaytimePainter painter)
        {
            if (!painter || !painter.enabled) return false;

            if (painter.meshEditing)
                return MeshEditorManager.target == painter && PainterCamera.Data.MeshTool.ShowGrid;

            if (!PlaytimePainter.IsCurrentTool || painter.LockTextureEditing || PainterCamera.Data.showConfig)
                return false;

            return painter.GlobalBrushType.NeedsGrid;
        }

        public static void AddIfNew<T>(this Dictionary<T, List<TextureMeta>> dic, T property, TextureMeta texture) where T : ShaderProperty.BaseShaderPropertyIndex
        {

            List<TextureMeta> mgmt;

            if (!dic.TryGetValue(property, out mgmt))
            {
                mgmt = new List<TextureMeta>();
                dic.Add(property, mgmt);
            }

            if (!mgmt.ContainsDuplicant(texture))
                mgmt.Add(texture);

        }

        public static bool TargetIsTexture2D(this TextureMeta id) => id != null && id.destination == TexTarget.Texture2D;

        public static bool TargetIsRenderTexture(this TextureMeta id) => id != null && id.destination == TexTarget.RenderTexture;

        public static TextureMeta GetImgDataIfExists(this Texture texture)
        {
            if (!texture || !PainterCamera.Data)
                return null;

            if (texture.IsBigRenderTexturePair() && PainterCamera.Inst.imgMetaUsingRendTex != null)
                return PainterCamera.Inst.imgMetaUsingRendTex;

            TextureMeta rid = null;

            var lst = PainterCamera.Data.imgMetas;

            if (lst == null) return rid;
            for (var i = 0; i < lst.Count; i++)
            {
                var id = lst[i];
                if ((texture != id.texture2D) && (texture != id.renderTexture) && (texture != id.other)) continue;

                rid = id;

                if (i > 3)
                    PainterCamera.Data.imgMetas.Move(i, 0);

                break;
            }

            return rid;
        }

        public static TextureMeta GetTextureMeta(this Texture texture)
        {
            if (!texture)
                return null;

            var nid = texture.GetImgDataIfExists() ?? new TextureMeta().Init(texture);

            return nid;
        }

        public static bool IsBigRenderTexturePair(this Texture tex) => tex && (tex == RenderTextureBuffersManager.GetPaintingBufferIfExist(0));

        private static bool ContainsDuplicant(this IList<TextureMeta> textures, TextureMeta other)
        {

            if (other == null)
                return true;

            for (var i = 0; i < textures.Count; i++)
                if (textures[i] == null) { textures.RemoveAt(i); i--; }

            return Enumerable.Contains(textures, other);
        }

        public static Texture GetDestinationTexture(this Texture texture)
        {
            var id = texture.GetImgDataIfExists();
            return id != null ? id.CurrentTexture() : texture;
        }

        public static RenderTexture CurrentRenderTexture(this TextureMeta id) => (id == null) ? null :
            (id.renderTexture ? id.renderTexture : PainterCamera.FrontBuffer);

        public static Texture ExclusiveTexture(this TextureMeta id)
        {
            if (id == null)
                return null;

            if (id.other != null)
                return id.other;

            switch (id.destination)
            {
                case TexTarget.RenderTexture:
                    return !id.renderTexture ? (Texture)id.texture2D : (Texture)id.renderTexture;
                case TexTarget.Texture2D:
                    return id.texture2D;
            }
            return null;
        }

        public static Texture CurrentTexture(this TextureMeta id)
        {
            if (id == null)
                return null;

            if (id.other)
                return id.other;

            switch (id.destination)
            {
                case TexTarget.RenderTexture:
                    if (id.renderTexture != null)
                        return id.renderTexture;
                    if (PainterCamera.Inst.imgMetaUsingRendTex == id)
                        return PainterCamera.FrontBuffer;
                    id.destination = TexTarget.Texture2D;
                    return id.texture2D;
                case TexTarget.Texture2D:
                    return id.texture2D;
            }
            return null;
        }

        public static MaterialMeta GetMaterialPainterMeta(this Material mat) => PainterCamera.Data?.GetMaterialDataFor(mat);

    }

}