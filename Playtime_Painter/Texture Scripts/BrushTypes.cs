using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter
{

    public abstract class BrushType : PainterSystem, IEditorDropdown, IPEGI, IGotDisplayName {

        private static List<BrushType> _allTypes;

        public static List<BrushType> AllTypes { get { InitIfNull(); return _allTypes; } }

        protected static void InitIfNull()
        {
            if (_allTypes != null) return;

            _allTypes = new List<BrushType>
            {
                new BrushTypeNormal(),
                new BrushTypeDecal(),
                new BrushTypeLazy(),
                new BrushTypeSphere(),
                new BrushTypePixel()
            };
            // _allTypes.Add(new BrushTypeSamplingOffset());

            //The code below can find all brushes, but at some Compilation time cost:
            /*
            List<GetBrushType> allTypes = CsharpFuncs.GetAllChildTypesOf<BrushType>();
            foreach (GetBrushType t in allTypes)
            {
                BrushType tb = (BrushType)Activator.CreateInstance(t);
                _allTypes.Add(tb);
            }
            */
        }

        protected static Vector2 UvToPosition(Vector2 uv) { return (uv - Vector2.one * 0.5f) * PainterCamera.OrthographicSize * 2; }

        public void SetKeyword(bool texcoord2)
        {

            foreach (var bs in AllTypes)
            {
                var name = bs.ShaderKeyword(true);
                if (name != null)
                    Shader.DisableKeyword(name);

                name = bs.ShaderKeyword(false);
                if (name != null)
                    Shader.DisableKeyword(name);
            }
            
            UnityUtils.SetShaderKeyword(ShaderKeyword(texcoord2), true);

        }

        protected virtual string ShaderKeyword(bool texcoord2) => null; 

        private static int _typesCount;
        public readonly int index;

        protected BrushType()
        {
            index = _typesCount;
            _typesCount++;
        }

        public virtual bool SupportedByTex2D => false;
        public virtual bool SupportedByRenderTexturePair => true;
        public virtual bool SupportedBySingleBuffer => true;
        public virtual bool IsAWorldSpaceBrush => false;
        public virtual bool SupportsAlphaBufferPainting => true;
        public virtual bool IsPixelPerfect => false;
        public virtual bool IsUsingDecals => false;
        public virtual bool StartPaintingTheMomentMouseIsDown => true;
        public virtual bool SupportedForTerrainRt => true;
        public virtual bool NeedsGrid => false;
        
        #region Inspect

        public virtual string NameForDisplayPEGI => Translation.GetText();

        public virtual string ToolTip => Translation.GetDescription(); //NameForDisplayPEGI + " (No Tooltip)";

        protected virtual MsgPainter Translation => MsgPainter.Unnamed;

        #if PEGI
        
        public virtual bool ShowInDropdown()
        {
            var p = PlaytimePainter.inspected;

            if (!p)
                return true;
            
            if (!SupportedForTerrainRt && p.terrain)
                return false;

            var id = InspectedImageMeta;

            if (id == null)
                return false;

            var br = InspectedBrush;

            if (br != null)
            {
                var blitMode = br.GetBlitMode(br.IsCpu(p));
                if (blitMode.NeedsWorldSpacePosition && !IsAWorldSpaceBrush)
                    return false;
               
            }

            return 
                
                (id.destination == TexTarget.Texture2D && SupportedByTex2D) ||
                (id.destination == TexTarget.RenderTexture &&
                ((SupportedByRenderTexturePair && !id.renderTexture)
                || (SupportedBySingleBuffer && id.renderTexture)));
        }

        public virtual bool Inspect()
        {
            


            if (BrushConfig.InspectedIsCpuBrush || !PainterCamera.Inst)
                return false;

            var changed = false;

            var br = InspectedBrush;

            var adv = InspectAdvanced;

            var p = InspectedPainter;

            if (BrushConfig.showAdvanced || InspectedBrush.useMask) {

                if (adv || br.useMask)
                    "Mask".toggleIcon ("Multiply Brush Speed By Mask Texture's alpha", ref br.useMask, true).changes(ref changed);

                if (br.useMask) {

                    pegi.selectOrAdd(ref br.selectedSourceMask, ref TexMGMTdata.masks).nl(ref changed);

                    if (adv)
                        "Mask greyscale".toggleIcon("Otherwise will use alpha", ref br.maskFromGreyscale).nl(ref changed);

                    if (br.flipMaskAlpha || adv)
                    "Flip Mask ".toggleIcon("Alpha = 1-Alpha", ref br.flipMaskAlpha).nl(ref changed);

                    if (!br.randomMaskOffset && adv)
                        "Mask Offset ".edit01(ref br.maskOffset).nl(ref changed);

                    if (br.randomMaskOffset || adv)
                        "Random Mask Offset".toggleIcon(ref br.randomMaskOffset).nl(ref changed);
                    
                    if (adv)
                    if ("Mask Tiling: ".edit(70, ref br.maskTiling, 1, 8).nl(ref changed))
                        br.maskTiling = Mathf.Clamp(br.maskTiling, 0.1f, 64);
                }
            }
    
            if (InspectAdvanced && p.NeedsGrid() && "Center Grid On Object".Click().nl())
                GridNavigator.onGridPos = p.transform.position;

            return changed;
        }
        #endif
        #endregion

        public virtual void PaintToTexture2D(PlaytimePainter painter, BrushConfig br, StrokeVector st) {

            var deltaUv = st.uvTo - st.uvFrom;
            
            if (deltaUv.magnitude > (0.2f + st.avgBrushSpeed * 3)) deltaUv = Vector2.zero; // This is made to avoid glitch strokes on seams
            else st.avgBrushSpeed = (st.avgBrushSpeed + deltaUv.magnitude) / 2;

            var alpha = Mathf.Clamp01(br.Speed * (Application.isPlaying ? Time.deltaTime : 0.1f));

            var worldSpace = painter.NeedsGrid();

            var id = painter.ImgMeta;

            var deltaPos = st.DeltaWorldPos;

            float steps = 1;

            if (id.disableContiniousLine)  {
               
                st.uvFrom = st.uvTo;
                st.posFrom = st.posTo;
               
            }
            else {

                var uvDist = (deltaUv.magnitude * id.width * 8 / br.Size(false));
                var worldDist = st.DeltaWorldPos.magnitude;

                steps = (int) Mathf.Max(1, worldSpace ? worldDist : uvDist);

                deltaUv /= steps;
                deltaPos /= steps;

                st.uvFrom += deltaUv;
                st.posFrom += deltaPos;
            }


            BlitFunctions.PaintTexture2DMethod blitMethod = null;
            
            foreach (var p in PainterSystemManagerPluginBase.BrushPlugins)
                if (p.IsEnabledFor(painter, id, br)) {
                    p.PaintPixelsInRam(st, alpha, id, br, painter); 
                    blitMethod = p.PaintPixelsInRam;
                    break;
                }

            if (blitMethod == null) {
                blitMethod = BlitFunctions.Paint;
                blitMethod(st, alpha, id, br, painter);
            }

            for (float i = 1; i < steps; i++) {
                st.uvFrom += deltaUv;
                st.posFrom += deltaPos;
                blitMethod(st, alpha, id, br, painter);
            }

            painter.AfterStroke(st);
        }

        public virtual void PaintRenderTexture(PlaytimePainter painter, BrushConfig br, StrokeVector st)
        {

            BeforeStroke(painter, br, st);

            if (st.CrossedASeam())
                st.uvFrom = st.uvTo;

            var id = painter.ImgMeta;
            
            bool alphaBuffer;

            TexMGMT.Shader_UpdateStrokeSegment(br, br.Speed * 0.05f, id, st, painter, out alphaBuffer);

            var rb = RtBrush;

            rb.localScale = Vector3.one;
            var direction = st.DeltaUv;
            var length = direction.magnitude;
            BrushMesh = PainterCamera.BrushMeshGenerator.GetLongMesh(length * 256, br.StrokeWidth(id.width, false));
            rb.localRotation = Quaternion.Euler(new Vector3(0, 0, (direction.x > 0 ? -1 : 1) * Vector2.Angle(Vector2.up, direction)));

            rb.localPosition = StrokeVector.BrushWorldPositionFrom((st.uvFrom + st.uvTo) *0.5f);

            TexMGMT.Render();

            AfterStroke_Painter(painter, br, st, alphaBuffer, id);


        }

        public void BeforeStroke(PlaytimePainter painter, BrushConfig br, StrokeVector st) {

            var cam = TexMGMT;

            if (!RenderTextureBuffersManager.secondBufferUpdated)
                RenderTextureBuffersManager.UpdateBufferTwo();
            
            foreach (var p in  painter.Plugins)
                p.BeforeGpuStroke(painter, br, st, this);
        }

        public virtual void AfterStroke_Painter(PlaytimePainter painter, BrushConfig br, StrokeVector st, bool alphaBuffer, ImageMeta id) {

            painter.AfterStroke(st);
            
            if (br.useMask && st.mouseUp && br.randomMaskOffset)
                br.maskOffset = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));
            
            if (alphaBuffer) {
                var sh = br.GetBlitMode(false).ShaderForAlphaBufferBlit;
                if (painter.NotUsingPreview) 
                    TexMGMT.UpdateFromAlphaBuffer(id.CurrentRenderTexture(), sh);
                else 
                    TexMGMT.AlphaBufferSetDirtyBeforeRender(id, sh);
            }
            else if (!br.IsSingleBufferBrush() && !br.IsA3DBrush(painter))
                TexMGMT.UpdateBufferSegment();

            foreach (var p in painter.Plugins)
                p.AfterGpuStroke(painter, br, st, this);
        }

        protected static void AfterStroke_NoPainter(BrushConfig br, bool alphaBuffer, RenderTexture rt = null) {

            if (br.useMask && br.randomMaskOffset)
                br.maskOffset = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));
            
            if (alphaBuffer) {
                var sh = br.GetBlitMode(false).ShaderForAlphaBufferBlit;
                TexMGMT.UpdateFromAlphaBuffer(rt, sh);
            }
        }

       

    }

    public class BrushTypePixel : BrushType
    {
        static BrushTypePixel _inst;
        public BrushTypePixel() { _inst = this; }
        public static BrushTypePixel Inst { get { InitIfNull(); return _inst; } }

        protected override string ShaderKeyword(bool texcoord2) => "BRUSH_SQUARE";

        public override bool SupportedByTex2D => true;

        protected override MsgPainter Translation => MsgPainter.BrushTypePixel;

        public override bool IsPixelPerfect => true; 

        public override void PaintRenderTexture(PlaytimePainter painter, BrushConfig br, StrokeVector st)
        {

           BeforeStroke(painter, br, st);

             if (st.CrossedASeam())
                 st.uvFrom = st.uvTo;

             if (RenderTextureBuffersManager.bigRtPair == null) TexMGMT.RecreateBuffersIfDestroyed();

             ImageMeta id = painter.ImgMeta;


             bool alphaBuffer;

            TexMGMT.Shader_UpdateStrokeSegment(br, br.Speed * 0.05f, id, st, painter, out alphaBuffer);

             RtBrush.localScale = Vector3.one * br.StrokeWidth(id.width, false);

             BrushMesh = PainterCamera.BrushMeshGenerator.GetQuad();
             RtBrush.localRotation = Quaternion.identity;

             RtBrush.localPosition = st.BrushWorldPosition;

             TexMGMT.Render();

             AfterStroke_Painter(painter, br, st, alphaBuffer, id);
        }

    }

    public class BrushTypeNormal : BrushType {

        static BrushTypeNormal _inst;
        public BrushTypeNormal() { _inst = this; }
        public static BrushTypeNormal Inst { get { InitIfNull(); return _inst; } }

        protected override string ShaderKeyword(bool texcoord) => "BRUSH_2D"; 

        public override bool SupportedByTex2D => true; 

        protected override MsgPainter Translation => MsgPainter.BrushTypeNormal;

        public static void Paint(Vector2 uv, BrushConfig br, RenderTexture rt)
        {

            if (RenderTextureBuffersManager.bigRtPair == null)
                TexMGMT.RecreateBuffersIfDestroyed();

            var id = rt.GetImgData();
            var stroke = new StrokeVector(uv) {
                firstStroke = false
            };


            bool alphaBuffer;

            TexMGMT.Shader_UpdateStrokeSegment(br, br.Speed * 0.05f, id, stroke, null, out alphaBuffer);

            float width = br.StrokeWidth(id.width, false);

            RtBrush.localScale = Vector3.one;

            BrushMesh = PainterCamera.BrushMeshGenerator.GetLongMesh(0, width);
            RtBrush.localRotation = Quaternion.Euler(new Vector3(0, 0, Vector2.Angle(Vector2.up, Vector2.zero)));

            RtBrush.localPosition = StrokeVector.BrushWorldPositionFrom(uv);

            TexMGMT.Render();

            AfterStroke_NoPainter(br,  alphaBuffer, rt);

        }
    }

    public class BrushTypeDecal : BrushType {

        private static BrushTypeDecal _inst;

        public BrushTypeDecal() { _inst = this; }

        public static BrushTypeDecal Inst { get { InitIfNull(); return _inst; } }

        public override bool SupportedBySingleBuffer => false;

        public override bool SupportedForTerrainRt => false;

        public override bool SupportsAlphaBufferPainting => false;

        protected override string ShaderKeyword(bool texcoord) => "BRUSH_DECAL"; 
        public override bool IsUsingDecals => true; 

        private Vector2 _previousUv;

        public override void PaintRenderTexture(PlaytimePainter painter, BrushConfig br, StrokeVector st)
        {

            BeforeStroke(painter, br, st);

            var id = painter.ImgMeta;

            if (st.firstStroke || br.decalContentious)
            {

                if (br.decalRotationMethod == DecalRotationMethod.StrokeDirection)
                {
                    var delta = st.uvTo - _previousUv;

                    var portion = Mathf.Clamp01(delta.magnitude * id.width * 4 / br.Size(false));

                    var newAngle = Vector2.SignedAngle(Vector2.up, delta) + br.decalAngleModifier;
                    br.decalAngle = Mathf.LerpAngle(br.decalAngle, newAngle, portion);

                    _previousUv = st.uvTo;
   
                }

                if (RenderTextureBuffersManager.bigRtPair == null) TexMGMT.RecreateBuffersIfDestroyed();


                bool alphaBuffer;

                TexMGMT.Shader_UpdateStrokeSegment(br, 1, id, st, painter, out alphaBuffer);
                var tf = RtBrush;
                tf.localScale = Vector3.one * br.Size(false);
                tf.localRotation = Quaternion.Euler(new Vector3(0, 0, br.decalAngle));
                BrushMesh = PainterCamera.BrushMeshGenerator.GetQuad();

                st.uvTo = st.uvTo.To01Space();

                var deltaUv = st.DeltaUv;

                var uv = st.uvTo;

                if (br.decalRotationMethod == DecalRotationMethod.StrokeDirection && !st.firstStroke)
                {
                    var length = Mathf.Max(deltaUv.magnitude * 2 * id.width / br.Size(false), 1);
                    var scale = tf.localScale;

                    if ((Mathf.Abs(Mathf.Abs(br.decalAngleModifier) - 90)) < 40)
                        scale.x *= length;
                    else
                        scale.y *= length;

                    tf.localScale = scale;
                    uv -= deltaUv * ((length - 1) * 0.5f / length);
                }

                tf.localPosition = StrokeVector.BrushWorldPositionFrom(uv);

                TexMGMT.Render();

                AfterStroke_Painter(painter, br, st, alphaBuffer, id);

            }
            else
                painter.AfterStroke(st);
        }

        public override void AfterStroke_Painter(PlaytimePainter painter, BrushConfig br, StrokeVector st, bool alphaBuffer, ImageMeta id)
        {
            base.AfterStroke_Painter(painter, br, st, alphaBuffer, id);

            if (br.decalRotationMethod != DecalRotationMethod.Random) return;
            
            br.decalAngle = Random.Range(-90f, 450f);
            TexMGMT.Shader_UpdateDecal(Cfg.brushConfig); 
        }

        #region Inspector

        protected override MsgPainter Translation => MsgPainter.BrushTypeDecal;

        #if PEGI
        public override bool Inspect()
        {

            var changed = false;

            pegi.select_Index(ref InspectedBrush.selectedDecal, TexMGMTdata.decals).changes(ref changed);

            var decal = TexMGMTdata.decals.TryGet(InspectedBrush.selectedDecal);

            if (decal == null)
                "Select a valid decal. You can add some in Config -> Lists.".fullWindowWarningDocumentationClick("No Decal selected");

            pegi.nl();



            "Continuous".toggle("Will keep adding decal every frame while the mouse is down", 80, ref InspectedBrush.decalContentious).changes(ref changed);

            "Continious Decal will keep painting every frame while mouse button is held".fullWindowDocumentationClick("Countinious Decal");

            pegi.nl();

            "Rotation".write("Rotation method", 60);

            pegi.editEnum(ref InspectedBrush.decalRotationMethod).nl(ref changed); 

            switch (InspectedBrush.decalRotationMethod)
            {
                case DecalRotationMethod.Set:
                    "Angle:".write("Decal rotation", 60);
                    changed |= pegi.edit(ref InspectedBrush.decalAngle, -90, 450);
                    break;
                case DecalRotationMethod.StrokeDirection:
                    "Ang Offset:".edit("Angle modifier after the rotation method is applied", 80, ref InspectedBrush.decalAngleModifier, -180f, 180f);
                    break;
            }

            pegi.newLine();
            if (!BrushExtensions.HasFlag(InspectedBrush.mask, BrushMask.A))
                "! Alpha chanel is disabled. Decals may not render properly".writeHint();

            return changed;

        }
        #endif
        #endregion
    }

    public class BrushTypeLazy : BrushType {
        private static BrushTypeLazy _inst;
        public BrushTypeLazy() { _inst = this; }
        public static BrushTypeLazy Inst { get { InitIfNull(); return _inst; } }

        public override bool StartPaintingTheMomentMouseIsDown => false; 
        protected override string ShaderKeyword(bool texcoord2) => "BRUSH_2D"; 

        private float _lazySpeedDynamic = 1;
        private float _lazyAngleSmoothed = 1;
        public Vector2 previousDirectionLazy;

        protected override MsgPainter Translation => MsgPainter.BrushTypeLazy;
        
        public override void PaintRenderTexture(PlaytimePainter painter, BrushConfig br, StrokeVector st)
        {

            BeforeStroke(painter, br, st);
 
            var deltaUv = st.DeltaUv;//uv - st.uvFrom;//.Previous_uv;
            var magnitude = deltaUv.magnitude;

            var id = painter.ImgMeta;

            var width = br.Size(false) / id.width * 4;

            var trackPortion = (deltaUv.magnitude - width * 0.5f) * 0.25f;

            if (!(trackPortion > 0) && !st.mouseUp) return;
            
            if (st.firstStroke)
            {
                previousDirectionLazy = st.previousDelta = deltaUv;
                _lazySpeedDynamic = deltaUv.magnitude;
                _lazyAngleSmoothed = 0;
                // Debug.Log("First stroke");
            }

            var angle = Mathf.Deg2Rad * Vector2.Angle(st.previousDelta, deltaUv);

            var smooth = angle < Mathf.PI * 0.5f;

            if (st.CrossedASeam() && (magnitude > previousDirectionLazy.magnitude * 8))
            {
                // Debug.Log("Crossed a seam");
                st.mouseUp = true;
                st.uvTo = st.uvFrom;// painter.Previous_uv;
                deltaUv = Vector2.zero;
                smooth = false;
            }

            previousDirectionLazy = deltaUv;
            
            if (!st.mouseUp)
            {
                if (smooth)
                {
                    var clockwise = Vector3.Cross(st.previousDelta, deltaUv).z > 0 ? 1f : -1f;
                    var sin = Mathf.Sin(angle) * clockwise;
                    float maxSinus = 8;
                    _lazyAngleSmoothed = Mathf.Abs(_lazyAngleSmoothed) > Mathf.Abs(sin) ? sin : Mathf.Lerp(_lazyAngleSmoothed, sin, 0.2f);
                    
                    sin = _lazyAngleSmoothed;

                    if ((sin * sin > maxSinus * maxSinus) || ((sin > 0) != (maxSinus > 0)))
                    {
                        var absSin = Mathf.Abs(sin);
                        var absNSin = Mathf.Abs(maxSinus);

                        if (absSin < absNSin) maxSinus = maxSinus * absSin / absNSin;

                        st.uvTo = st.uvFrom + st.previousDelta.normalized.Rotate_Radians(maxSinus * clockwise) * trackPortion;
                        _lazySpeedDynamic = trackPortion;
                    }
                    else
                    {
                        _lazySpeedDynamic = Mathf.Min(deltaUv.magnitude * 0.5f, Mathf.Lerp(_lazySpeedDynamic, deltaUv.magnitude * 0.5f, 0.001f));

                        _lazySpeedDynamic = Mathf.Max(trackPortion, _lazySpeedDynamic);
                        st.uvTo = st.uvFrom + st.previousDelta.normalized.Rotate_Radians(sin) * _lazySpeedDynamic;
                    }
                }
                else
                {
                    _lazySpeedDynamic = deltaUv.magnitude;
                    _lazyAngleSmoothed = 0;
                    st.uvTo = st.uvFrom + deltaUv.normalized * trackPortion;
                }
            }
            var r = TexMGMT;
            
            if (RenderTextureBuffersManager.bigRtPair == null)
                TexMGMT.RecreateBuffersIfDestroyed();

            var meshWidth = br.StrokeWidth(id.width, false); 
            
            var tf = RtBrush;

            var direction = st.DeltaUv;

            var isTail = st.firstStroke;

            bool alphaBuffer;

            if (!isTail && !smooth)
            {
                var st2 = new StrokeVector(st)
                {
                    firstStroke = false
                };
                r.Shader_UpdateStrokeSegment(br, br.Speed * 0.05f, id, st2, painter, out alphaBuffer);

                Vector3 junkPoint = st.uvFrom + st.previousDelta * 0.01f;
                BrushMesh = PainterCamera.BrushMeshGenerator.GetStreak(UvToPosition(st.uvFrom), UvToPosition(junkPoint), meshWidth, true, false);
                tf.localScale = Vector3.one;
                tf.localRotation = Quaternion.identity;
                tf.localPosition = new Vector3(0, 0, 10);


                r.Render();
                st.uvFrom = junkPoint;
                isTail = true;
            }

            r.Shader_UpdateStrokeSegment(br, br.Speed * 0.05f, id, st, painter, out alphaBuffer);

            BrushMesh = PainterCamera.BrushMeshGenerator.GetStreak(UvToPosition(st.uvFrom), UvToPosition(st.uvTo), meshWidth, st.mouseUp, isTail);
            tf.localScale = Vector3.one;
            tf.localRotation = Quaternion.identity;
            tf.localPosition = new Vector3(0, 0, 10);

            st.previousDelta = direction;

            r.Render();

            AfterStroke_Painter(painter, br, st, alphaBuffer, id);
        }
    }

    public class BrushTypeSphere : BrushType {

        static BrushTypeSphere _inst;

        public BrushTypeSphere() { _inst = this; }

        public static BrushTypeSphere Inst { get { InitIfNull(); return _inst; } }

        protected override string ShaderKeyword(bool texcoord2) => texcoord2 ? "BRUSH_3D_TEXCOORD2" : "BRUSH_3D";

        public override bool IsAWorldSpaceBrush => true;
        
        public override bool SupportsAlphaBufferPainting => true;

        public override bool SupportedForTerrainRt => false; 

        public override bool NeedsGrid => Cfg.useGridForBrush; 

        private static void PrepareSphereBrush(ImageMeta id, BrushConfig br, StrokeVector stroke, PlaytimePainter painter, out bool alphaBuffer)
        {
            if (RenderTextureBuffersManager.bigRtPair.IsNullOrEmpty())
                TexMGMT.RecreateBuffersIfDestroyed();

            if (stroke.mouseDwn)
                stroke.posFrom = stroke.posTo;

            TexMGMT.Shader_UpdateStrokeSegment(br, br.Speed * 0.05f, id, stroke, painter, out alphaBuffer);

            var offset = id.offset - stroke.unRepeatedUv.Floor();

            stroke.SetWorldPosInShader();

            PainterDataAndConfig.BRUSH_EDITED_UV_OFFSET.GlobalValue = new Vector4(id.tiling.x, id.tiling.y, offset.x, offset.y);
            PainterDataAndConfig.BRUSH_ATLAS_SECTION_AND_ROWS.GlobalValue = new Vector4(0, 0, 1, 0);
        }

        public override void PaintRenderTexture(PlaytimePainter painter, BrushConfig br, StrokeVector st) {

            var id = painter.ImgMeta;

            BeforeStroke(painter, br, st);

            bool alphaBuffer;

            PrepareSphereBrush(id, br, st, painter, out alphaBuffer);

            if (!st.mouseDwn)
            {
                TexMGMT.brushRenderer.UseMeshAsBrush(painter);
                TexMGMT.Render();
            }

            AfterStroke_Painter(painter, br, st, alphaBuffer, id);
        }

        public static void Paint(RenderTexture rt, GameObject go, SkinnedMeshRenderer skinner, BrushConfig br, StrokeVector st, int subMeshIndex)
        {
            br.GetBlitMode(false).PrePaint(null, br, st);

            bool alphaBuffer;

            PrepareSphereBrush(rt.GetImgData(), br, st, null, out alphaBuffer);
            TexMGMT.brushRenderer.UseSkinMeshAsBrush(go, skinner, subMeshIndex);
            TexMGMT.Render();
            AfterStroke_NoPainter(br, alphaBuffer, rt);
        }

        public static void Paint(RenderTexture rt, GameObject go, Mesh mesh, BrushConfig br, StrokeVector st, List<int> subMeshIndex)
        {
            br.GetBlitMode(false).PrePaint(null, br, st);

            bool alphaBuffer;
            
            PrepareSphereBrush(rt.GetImgData(), br, st, null, out alphaBuffer);
            TexMGMT.brushRenderer.UseMeshAsBrush(go, mesh, subMeshIndex);
            TexMGMT.Render();
            AfterStroke_NoPainter(br, alphaBuffer, rt);
        }

        public static void PaintAtlased(RenderTexture rt, GameObject go, Mesh mesh, BrushConfig br, StrokeVector st, List<int> subMeshIndex, int aTexturesInRow)
        {

            br.GetBlitMode(false).PrePaint(null, br, st);

            PainterDataAndConfig.BRUSH_ATLAS_SECTION_AND_ROWS.GlobalValue = new Vector4(0, 0, aTexturesInRow, 1);

            bool alphaBuffer;
            PrepareSphereBrush(rt.GetImgData(), br, st, null, out alphaBuffer);
            TexMGMT.brushRenderer.UseMeshAsBrush(go, mesh, subMeshIndex);
            TexMGMT.Render();

            AfterStroke_NoPainter(br, alphaBuffer, rt);

            PainterDataAndConfig.BRUSH_ATLAS_SECTION_AND_ROWS.GlobalValue = new Vector4(0, 0, 1, 0);
        }

        #region Inspector

        protected override MsgPainter Translation => MsgPainter.BrushTypeSphere;

        #if PEGI
        public override bool Inspect()
        {

            var changed = false;

            var br = InspectedBrush;

            if (InspectAdvanced || Cfg.useGridForBrush)
                "Paint On Grid".toggleIcon(ref Cfg.useGridForBrush, true).nl();

            if (!br.useAlphaBuffer && (br.worldSpaceBrushPixelJitter || InspectAdvanced)) {
                "One Pixel Jitter".toggleIcon(ref br.worldSpaceBrushPixelJitter).changes(ref changed);
                "Will provide a single pixel jitter which can help fix seams not being painted properly"
                    .fullWindowDocumentationClick("Why use one pixel jitter?");
                pegi.nl();
            }
        
            base.Inspect().nl(ref changed);

            return changed;
        }
        #endif
        #endregion
    }

   


}