using System.Collections.Generic;
using System.Linq;
using QuizCanners.Inspect;
using PainterTool.MeshEditing;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool 
{
    public static class RenderTextureBuffersManager {

        public static SO_PainterDataAndConfig Data => Painter.Data;

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
            if (alphaBufferTexture) 
            {
                alphaBufferTexture.DiscardContents();
                Blit(Color.clear, alphaBufferTexture);
            }
        }

        public static int bigRtVersion;

        public static bool secondBufferUpdated = true;

        public static void UpdateSecondBuffer()
        {
            if (secondBufferUpdated)
                return;

            if (!bigRtPair.IsNullOrEmpty() && bigRtPair[0]) {
                bigRtPair[1].DiscardContents();
                Graphics.Blit(bigRtPair[0], bigRtPair[1]);
            } else
                logger.Log("Render Texture buffers are null");

            secondBufferUpdated = true;
            bigRtVersion++;
        }

        private static RenderTextureFormat GetTextureFormat() => 
            SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.ARGB32;
        
        public static void InitBrushBuffers()
        {
            if (!GotPaintingBuffers)
            {

                #if debugInits
                Debug.Log("Creating Painting buffers");
                #endif
                
                bigRtPair = new RenderTexture[2];

                var format = GetTextureFormat();
                var tA = new RenderTexture(renderBuffersSize, renderBuffersSize, 0, format, RenderTextureReadWrite.Linear);
                var tB = new RenderTexture(renderBuffersSize, renderBuffersSize, 0, format, RenderTextureReadWrite.Linear);

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
                alphaBufferTexture = new RenderTexture(renderBuffersSize, renderBuffersSize, 0, GetTextureFormat(), RenderTextureReadWrite.Linear)
                {
                    wrapMode = TextureWrapMode.Repeat,
                    name = "Painting Alpha Buffer _ " + renderBuffersSize,
                    useMipMap = false
                };
            }
        }

        public static bool GotPaintingBuffers => !bigRtPair.IsNullOrEmpty();

        private static void DestroyPaintingBuffers()
        {
            if (bigRtPair!= null)
                foreach (var b in bigRtPair)
                    b.DestroyWhatever();

            bigRtPair = null;

            alphaBufferTexture.DestroyWhatever();
        }

        public static void RefreshPaintingBuffers()
        {
            Painter.Camera.EmptyBufferTarget();
            DestroyPaintingBuffers();
            GetOrCreatePaintingBuffers();
        }

        #endregion

        #region Scaling Buffers
        private const int squareBuffersCount = 13;
        
        private static readonly RenderTexture[] _squareBuffers = new RenderTexture[squareBuffersCount];

        private static readonly List<RenderTexture> nonSquareBuffers = new();
        public static RenderTexture GetNonSquareBuffer(int width, int height)
        {
            foreach (RenderTexture r in nonSquareBuffers)
                if ((r.width == width) && (r.height == height)) return r;

            RenderTexture rt = new(width, height, 0, Data.useFloatForScalingBuffers ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.ARGB32
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
            foreach (var textureToDestroy in _squareBuffers)
                textureToDestroy.DestroyWhateverUnityObject();

            foreach (var textureToDestroy in nonSquareBuffers)
                textureToDestroy.DestroyWhateverUnityObject();

            for (int i = 0; i < _squareBuffers.Length; i++)
                _squareBuffers[i] = null;

            nonSquareBuffers.Clear();
        }

        private static RenderTexture SquareBuffer(int width) => GetSquareBuffer(width);

        public static RenderTexture GetDownscaledBigRt(int width, int height, bool allowApprox = false) => Downscale_ToBuffer(GetOrCreatePaintingBuffers()[0], width, height, allowApprox: allowApprox);
        
        public static RenderTexture GetDownscaleOf(Texture texture, int targetSize, bool allowApprox = false)
        {

            if (!texture)
                logger.Log("Null texture as downscale source");
            else if (texture.width != texture.height)
                logger.Log("Texture should be square");
            else if (!Mathf.IsPowerOfTwo(texture.width))
                logger.Log("{0} is not a Power of two".F(texture));
            else
                return Downscale_ToBuffer(texture, targetSize, targetSize, allowApprox: allowApprox);


            return null;
        }

        private static RenderTexture Render(Texture src, RenderTexture trg, Material mat)
        {
            PainterShaderVariables.SourceTextureSize = src;
            return Painter.Camera.Render(src, trg, mat);

        }

        private static RenderTexture Render(Texture src, RenderTexture trg, Shader shd)
        {
            PainterShaderVariables.SourceTextureSize = src;
            return Painter.Camera.Render(src, trg, shd);

        }

        private static RenderTexture Downscale_ToBuffer(Texture textureToDownscale, int width, int height, Material material = null, Shader shader = null, bool allowApprox = false)
        {

            if (!textureToDownscale)
                return null;

            bool usingCustom = material || shader;

            if (!shader)
                shader = Data.pixPerfectCopy.Shader;
            
            bool square = (width == height);
            if (!square || !Mathf.IsPowerOfTwo(width))
                return Render(textureToDownscale, GetNonSquareBuffer(width, height), shader);
            

            int tmpWidth = Mathf.Max(textureToDownscale.width / 2, width);

            RenderTexture srcRt = material
                ? Render(textureToDownscale, SquareBuffer(tmpWidth), material)
                : Render(textureToDownscale, SquareBuffer(tmpWidth), shader);
                
            while (tmpWidth > width) {

                bool jobDone = false;

                var previousFrom = srcRt;

                if (!usingCustom) {

                    if (allowApprox) {

                        if (tmpWidth / 32 > width) {

                            tmpWidth /= 64;
                            srcRt = Render(srcRt, SquareBuffer(tmpWidth), Data.bufferCopyDownscaleX64_Approx);
                            jobDone = true;

                        } else if (tmpWidth / 16 > width) {

                            tmpWidth /= 32;
                            srcRt = Render(srcRt, SquareBuffer(tmpWidth), Data.bufferCopyDownscaleX32_Approx);
                            jobDone = true;

                        }
                        else if (tmpWidth / 8 > width) {

                            tmpWidth /= 16;
                            srcRt = Render(srcRt, SquareBuffer(tmpWidth), Data.bufferCopyDownscaleX16_Approx);
                            jobDone = true;

                        }
                    }

                    if (!jobDone) {
                        if (tmpWidth / 4 > width)
                        {
                            tmpWidth /= 8;
                            srcRt = Render(srcRt, SquareBuffer(tmpWidth), Data.bufferCopyDownscaleX8);
                            jobDone = true;
                        }
                        else if (tmpWidth / 2 > width)
                        {
                            tmpWidth /= 4;
                            srcRt = Render(srcRt, SquareBuffer(tmpWidth), Data.bufferCopyDownscaleX4);
                            jobDone = true;
                        }
                    }
                }

                if (!jobDone) {
                    tmpWidth /= 2;
                    srcRt = material
                        ? Render(srcRt, SquareBuffer(tmpWidth), material)
                        : Render(srcRt, SquareBuffer(tmpWidth), shader);
                }

                previousFrom.DiscardContents();
            }

            return srcRt;
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

        private static RenderTexture GetDepthRenderTexture(int sz) => new(sz, sz, 32, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear)
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
        
        public static void InspectDepthTarget()
        {
            "Size of Depth buffers".PegiLabel().Edit(ref sizeOfDepthBuffers);

            if (depthTarget && depthTarget.width!=sizeOfDepthBuffers && Icon.Done.Click("Recreate Depth Texture").Nl())
            {
                DestroyDepthBuffers();
                UpdateDepthTarget();
            }
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
                {
                    tmpMaterial = new Material(Painter.Data.pixPerfectCopy.Shader)
                    {
                        name = "TMP Material"
                    };
                }
                return tmpMaterial;
            }
        }

        private static Material TempMaterial(Shader shade)
        {
            var mat = ReusableMaterial;
            mat.shader = shade;

            return mat;
        }

        internal static RenderTexture Blit(Texture texture, TextureMeta id)
        {
            if (!texture || id == null)
                return null;

            return Blit(texture, id.CurrentRenderTexture());
        }

        public static RenderTexture Blit(Texture from, RenderTexture to) => Blit(from, to, Data.pixPerfectCopy.Shader);

        public static RenderTexture Blit(Texture from, RenderTexture to, Shader shader) => Blit(from, to, TempMaterial(shader));

        public static RenderTexture Blit(Texture from, RenderTexture to, Material blitMaterial) {
            PainterShaderVariables.SourceTextureSize = from;
            Graphics.Blit(from, to, blitMaterial);
            AfterBlit(to);
            return to;
        }

        private static void AfterBlit(Texture target) {
            if (target && target.IsBigRenderTexturePair())
                secondBufferUpdated = false;

            Singleton_PainterCamera.lastPainterCall = QcUnity.TimeSinceStartup();
        }

        public static void Blit(Color col, RenderTexture renderTexture)
        {
            renderTexture.Clear(col);
            /*
            RenderTexture rt = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, col);
            RenderTexture.active = rt;
            */
           
            /*
            if (!Data)
            {
                Debug.LogError("No Playtime painter for fill operation");
                return to;
            }
            PainterShaderVariables.BrushColorProperty.GlobalValue = col;
            Blit(Data.bufferColorFill, to);

            return to;*/
        }

        public static RenderTexture Blit(Shader shader, RenderTexture target) 
        {
            var mat = TempMaterial(shader);
            Graphics.Blit(null, target, mat);
            return target;
        }

        #endregion

        public static void BlitGL(Texture source, RenderTexture destination, Material mat) => RenderTextureBlit.BlitGL(source, destination, mat);
           
        public static void OnDisable() {
            DestroyScalingBuffers();
            DestroyPaintingBuffers();
            DestroyDepthBuffers();
            DestroyBuffersWithDepth();
        }

        #region Inspector

        private static readonly QcLog.ChillLogger logger = new("Buffers Mgmt");

        //private static int inspectedElement = -1;
        private static readonly pegi.EnterExitContext context = new();

        public static void Inspect() {

            using (context.StartContext())
            {
                var changed = pegi.ChangeTrackStart();

                if ("Refresh Buffers".PegiLabel().Click().Nl())
                    RefreshPaintingBuffers();

                if ("Panting Buffers".PegiLabel().IsEntered().Nl())
                {

                    "ARGBfloat supported: {0}".F(GetTextureFormat()).PegiLabel().Nl();

                    if (GotPaintingBuffers && Icon.Delete.Click())
                        DestroyPaintingBuffers();

                    if ("Buffer Size".PegiLabel("Size of Buffers used for GPU painting", 90).SelectPow2(ref renderBuffersSize, 64, 4096).Nl())
                        RefreshPaintingBuffers();

                    if (GotPaintingBuffers)
                    {
                        pegi.Draw(bigRtPair[0], 200);
                        pegi.Nl();
                        pegi.Draw(bigRtPair[1], 200);

                        pegi.Nl();
                    }

                    "Depth".PegiLabel().Edit(ref alphaBufferTexture).Nl();
                }

                if ("Scaling Buffers".PegiLabel().IsEntered().Nl())
                {
                    if (Icon.Delete.Click().Nl())
                        DestroyScalingBuffers();

                    if ("Use RGBAFloat for scaling".PegiLabel().ToggleIcon(ref Data.useFloatForScalingBuffers).Nl())
                        DestroyScalingBuffers();

                    for (int i = 0; i < squareBuffersCount; i++)
                    {

                        if (!_squareBuffers[i])
                            "No Buffer for {0}*{0}".F(Mathf.Pow(2, i)).PegiLabel().Nl();
                        else
                        {

                            pegi.Edit(ref _squareBuffers[i]).Nl();
                            pegi.Draw(_squareBuffers[i], 250);
                        }

                        pegi.Nl();
                    }
                }

                if ("Depth Texture".PegiLabel().IsEntered().Nl())
                {

                    if (Icon.Delete.Click())
                        DestroyDepthBuffers();

                    "For Camera".PegiLabel().Edit(ref depthTarget).Nl();
                    pegi.Draw(depthTarget, 250);
                    pegi.Nl();

                    "Reusable for blits".PegiLabel().Edit(ref _depthTargetForUsers).Nl();
                    pegi.Draw(_depthTargetForUsers, 250);
                }

                if ("Render Textures with Depth buffer".PegiLabel().IsEntered().Nl())
                {
                    if (Icon.Delete.Click())
                        DestroyBuffersWithDepth();

                    "Reusable".PegiLabel().Edit(ref renderTextureWithDepth).Nl();
                    pegi.Draw(renderTextureWithDepth, 250);
                }

                if (changed)
                    Data.SetToDirty();
            }
        }
        
        #endregion

    }



    public static class TextureEditorExtensionFunctions
    {

        public static void TeachingNotification(this string text)
        {
            if (Painter.Data && Painter.Data.showTeachingNotifications)
                pegi.GameView.ShowNotification(text);
        }

        public static Mesh GetMesh(this PainterComponent p)
        {
            if (!p) return null;

            if (p.skinnedMeshRenderer)
                return p.colliderForSkinnedMesh;

            return p.SharedMesh;

        }

        public static float StrokeWidth(this Brush br, float pixWidth, bool world) => br.Size(world) / (pixWidth) * 2 * Singleton_PainterCamera.OrthographicSize;

        public static bool IsSingleBufferBrush(this Brush b) => 
            (Painter.IsLinearColorSpace 
            && b.GetBlitMode(TexTarget.RenderTexture).SupportedBySingleBuffer
            && b.GetBrushType(TexTarget.RenderTexture).SupportedBySingleBuffer 
            && b.PaintingRGB);

        public static bool IsProjected(this Material mat) => mat && mat.shaderKeywords.Contains(PainterShaderVariables.UV_PROJECTED);

        public static bool NeedsGrid(this PainterComponent painter)
        {
            if (!painter || !painter.enabled) return false;

            if (painter.meshEditing)
                return MeshPainting.target == painter && Painter.Data.MeshTool.ShowGrid;

            if (!PainterComponent.IsCurrentTool || painter.TextureEditingBlocked || Painter.Data.showConfig)
                return false;

            return painter.GlobalBrushType.NeedsGrid;
        }

        internal static void AddIfNew<T>(this Dictionary<T, List<TextureMeta>> dic, T property, TextureMeta texture) where T : ShaderProperty.BaseShaderPropertyIndex
        {


            if (!dic.TryGetValue(property, out List<TextureMeta> mgmt))
            {
                mgmt = new List<TextureMeta>();
                dic.Add(property, mgmt);
            }

            if (!mgmt.ContainsDuplicant(texture))
                mgmt.Add(texture);

        }

        internal static bool TargetIsTexture2D(this TextureMeta id) => id != null && id.Target == TexTarget.Texture2D;

        public static bool TargetIsRenderTexture(this TextureMeta id) => id != null && id.Target == TexTarget.RenderTexture;

        internal static TextureMeta GetImgDataIfExists(this Texture texture)
        {
            if (!texture || !Painter.Data)
                return null;

            if (texture.IsBigRenderTexturePair() && Painter.Camera.imgMetaUsingRendTex != null)
                return Painter.Camera.imgMetaUsingRendTex;

          

            var lst = Painter.Data.imgMetas;

            if (lst == null) 
                return null;
            
            TextureMeta rid = null;
            
            for (var i = 0; i < lst.Count; i++)
            {
                var id = lst[i];
                if ((texture != id.Texture2D) && (texture != id.RenderTexture) && (texture != id.OtherTexture)) continue;

                rid = id;

                if (i > 3)
                    Painter.Data.imgMetas.Move(i, 0);

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

        internal static RenderTexture CurrentRenderTexture(this TextureMeta id) => (id == null) ? null :
            (id.RenderTexture ? id.RenderTexture : Singleton_PainterCamera.FrontBuffer);

        internal static Texture ExclusiveTexture(this TextureMeta id)
        {
            if (id == null)
                return null;

            if (id.OtherTexture != null)
                return id.OtherTexture;

            return id.Target switch
            {
                TexTarget.RenderTexture => !id.RenderTexture ? id.Texture2D : (Texture)id.RenderTexture,
                TexTarget.Texture2D => id.Texture2D,
                _ => null,
            };
        }

        internal static Texture CurrentTexture(this TextureMeta id)
        {
            if (id == null)
                return null;

            if (id.OtherTexture)
                return id.OtherTexture;

            switch (id.Target)
            {
                case TexTarget.RenderTexture:
                    if (id.RenderTexture != null)
                        return id.RenderTexture;
                    if (Painter.Camera.imgMetaUsingRendTex == id)
                        return Singleton_PainterCamera.FrontBuffer;
                    id.Target = TexTarget.Texture2D;
                    return id.Texture2D;
                case TexTarget.Texture2D:
                    return id.Texture2D;
            }
            return null;
        }

        internal static MaterialMeta GetMaterialPainterMeta(this Material mat)
        {
             if (!Painter.Data)
                 return null;
         
             return Painter.Data.GetMaterialDataFor(mat);
        }
    }

}