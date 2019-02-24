using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter
{

    public abstract class BrushType : PainterStuff, IEditorDropdown, IPEGI, IGotDisplayName {

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
            List<Type> allTypes = CsharpFuncs.GetAllChildTypesOf<BrushType>();
            foreach (Type t in allTypes)
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
            
            UnityHelperFunctions.SetShaderKeyword(ShaderKeyword(texcoord2), true);

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
        public virtual bool IsA3DBrush => false;
        public virtual bool IsPixelPerfect => false;
        public virtual bool IsUsingDecals => false;
        public virtual bool StartPaintingTheMomentMouseIsDown => true;
        public virtual bool SupportedForTerrainRt => true;
        public virtual bool NeedsGrid => false;

        public abstract string NameForDisplayPEGI { get; }

        public virtual string ToolTip => NameForDisplayPEGI + " (No Tooltip)";

        #region Inspect
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

            
            if (TexMGMTdata.masks.Count > 0)
            {
                pegi.nl();
                pegi.space();
                pegi.nl();

               "Mask".toggleIcon ("Multiply Brush Speed By Mask Texture's alpha", ref InspectedBrush.useMask, true).changes(ref changed);

                if (InspectedBrush.useMask) {

                    pegi.selectOrAdd(ref InspectedBrush.selectedSourceMask, ref TexMGMTdata.masks).nl(ref changed);

                    if (!InspectedBrush.randomMaskOffset)
                        "Mask Offset ".edit(ref InspectedBrush.maskOffset).nl(ref changed);

                    "Random Mask Offset".toggleIcon(ref InspectedBrush.randomMaskOffset, true).nl(ref changed);

          
                    if ("Mask Tiling: ".edit(70, ref InspectedBrush.maskTiling, 1, 8).nl(ref changed))
                        InspectedBrush.maskTiling = Mathf.Clamp(InspectedBrush.maskTiling, 0.1f, 64);
                    

                    "Flip Mask Alpha".toggleIcon("Alpha = 1-Alpha", ref InspectedBrush.flipMaskAlpha).nl(ref changed);
                    
                }


            }
    
            if (InspectedPainter.NeedsGrid() && "Center Grid On Object".Click().nl())
                GridNavigator.onGridPos = InspectedPainter.transform.position;

            return changed;
        }
        #endif
        #endregion

        public virtual void PaintToTexture2D(PlaytimePainter painter, BrushConfig br, StrokeVector st) {

            var deltaUv = st.uvTo - st.uvFrom;
            
            if (deltaUv.magnitude > (0.2f + st.avgBrushSpeed * 3)) deltaUv = Vector2.zero; // This is made to avoid glitch strokes on seams
            else st.avgBrushSpeed = (st.avgBrushSpeed + deltaUv.magnitude) / 2;

            var alpha = Mathf.Clamp01(br.speed * (Application.isPlaying ? Time.deltaTime : 0.1f));

            var worldSpace = painter.NeedsGrid();

            var id = painter.ImgMeta;

            var uvDist = (deltaUv.magnitude * id.width * 8 / br.Size(false));
            var worldDist = st.DeltaWorldPos.magnitude;

            float steps = (int)Mathf.Max(1, worldSpace ? worldDist : uvDist);

            deltaUv /= steps;
            var deltaPos = st.DeltaWorldPos / steps;

            st.uvFrom += deltaUv;
            st.posFrom += deltaPos;
            
            BlitFunctions.PaintTexture2DMethod blitMethod = null;
            
            foreach (var p in PainterManagerPluginBase.BrushPlugins)
                if (p.PaintPixelsInRam(st, alpha, id, br, painter)) {
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

            ImageMeta id = painter.ImgMeta;

            TexMGMT.Shader_UpdateStrokeSegment(br, br.speed * 0.05f, id, st, painter);

            var rb = RtBrush;

            rb.localScale = Vector3.one;
            Vector2 direction = st.DeltaUv;
            float length = direction.magnitude;
            BrushMesh = PainterCamera.BrushMeshGenerator.GetLongMesh(length * 256, br.StrokeWidth(id.width, false));
            rb.localRotation = Quaternion.Euler(new Vector3(0, 0, (direction.x > 0 ? -1 : 1) * Vector2.Angle(Vector2.up, direction)));

            rb.localPosition = StrokeVector.BrushWorldPositionFrom((st.uvFrom + st.uvTo) / 2);

            TexMGMT.Render();

            AfterStroke(painter, br, st);
        }

        protected static void AfterStroke(BrushConfig br)
        {

            if (br.useMask && br.randomMaskOffset)
                br.maskOffset = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));

        }

        public void BeforeStroke(PlaytimePainter painter, BrushConfig br, StrokeVector st) {

            var cam = TexMGMT;

            if (!cam.secondBufferUpdated)
                cam.UpdateBufferTwo();

            foreach (var p in painter.plugins)
                p.BeforeGpuStroke(painter, br, st, this);
        }

        public virtual void AfterStroke(PlaytimePainter painter, BrushConfig br, StrokeVector st)
        {

            painter.AfterStroke(st);

            if (!br.IsSingleBufferBrush() && !br.IsA3DBrush(painter))
                TexMGMT.UpdateBufferSegment();

            if (br.useMask && st.mouseUp && br.randomMaskOffset)
                br.maskOffset = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));

            foreach (var p in painter.plugins)
                p.AfterGpuStroke(painter, br, st, this);

        }

    }

    public class BrushTypePixel : BrushType
    {
        static BrushTypePixel _inst;
        public BrushTypePixel() { _inst = this; }
        public static BrushTypePixel Inst { get { InitIfNull(); return _inst; } }

        protected override string ShaderKeyword(bool texcoord2) => "BRUSH_SQUARE";

        public override bool SupportedByTex2D => true; 

        public override string NameForDisplayPEGI=> "Pixel"; 

        public override bool IsPixelPerfect => true; 

        public override void PaintRenderTexture(PlaytimePainter painter, BrushConfig br, StrokeVector st)
        {

           BeforeStroke(painter, br, st);

             if (st.CrossedASeam())
                 st.uvFrom = st.uvTo;

             if (TexMGMT.bigRtPair == null) TexMGMT.UpdateBuffersState();

             ImageMeta id = painter.ImgMeta;

             TexMGMT.Shader_UpdateStrokeSegment(br, br.speed * 0.05f, id, st, painter);

             RtBrush.localScale = Vector3.one * br.StrokeWidth(id.width, false);

             BrushMesh = PainterCamera.BrushMeshGenerator.GetQuad();
             RtBrush.localRotation = Quaternion.identity;

             RtBrush.localPosition = st.BrushWorldPosition;

             TexMGMT.Render();

             AfterStroke(painter, br, st);
        }

    }

    public class BrushTypeNormal : BrushType {

        static BrushTypeNormal _inst;
        public BrushTypeNormal() { _inst = this; }
        public static BrushTypeNormal Inst { get { InitIfNull(); return _inst; } }

        protected override string ShaderKeyword(bool texcoord) => "BRUSH_2D"; 

        public override bool SupportedByTex2D => true; 

        public override string NameForDisplayPEGI => "Normal";
        
        public static void Paint(Vector2 uv, BrushConfig br, RenderTexture rt)
        {

            if (TexMGMT.bigRtPair == null)
                TexMGMT.UpdateBuffersState();

            var id = rt.GetImgData();
            var stroke = new StrokeVector(uv) {
                firstStroke = false
            };

            TexMGMT.Shader_UpdateStrokeSegment(br, br.speed * 0.05f, id, stroke, null);

            float width = br.StrokeWidth(id.width, false);

            RtBrush.localScale = Vector3.one;

            BrushMesh = PainterCamera.BrushMeshGenerator.GetLongMesh(0, width);
            RtBrush.localRotation = Quaternion.Euler(new Vector3(0, 0, Vector2.Angle(Vector2.up, Vector2.zero)));

            RtBrush.localPosition = StrokeVector.BrushWorldPositionFrom(uv);

            TexMGMT.Render();

            AfterStroke(br);

        }
    }

    public class BrushTypeDecal : BrushType {

        private static BrushTypeDecal _inst;

        public BrushTypeDecal() { _inst = this; }

        public static BrushTypeDecal Inst { get { InitIfNull(); return _inst; } }

        public override bool SupportedBySingleBuffer => false;

        public override bool SupportedForTerrainRt => false;

        protected override string ShaderKeyword(bool texcoord) => "BRUSH_DECAL"; 
        public override bool IsUsingDecals => true; 

        public override string NameForDisplayPEGI => "Decal";

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

                if (TexMGMT.bigRtPair == null) TexMGMT.UpdateBuffersState();

                TexMGMT.Shader_UpdateStrokeSegment(br, 1, id, st, painter);
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

                AfterStroke(painter, br, st);

            }
            else
                painter.AfterStroke(st);
        }

        public override void AfterStroke(PlaytimePainter painter, BrushConfig br, StrokeVector st)
        {
            base.AfterStroke(painter, br, st);

            if (br.decalRotationMethod != DecalRotationMethod.Random) return;
            
            br.decalAngle = Random.Range(-90f, 450f);
            TexMGMT.Shader_UpdateDecal(Cfg.brushConfig); 
        }
        #if PEGI
        public override bool Inspect()
        {

            var changes = false;

            changes |= pegi.select(ref InspectedBrush.selectedDecal, TexMGMTdata.decals).nl();

            var decal = TexMGMTdata.decals.TryGet(InspectedBrush.selectedDecal);

            if (decal == null)
                "Select valid decal; Assign to Painter Camera.".write();
            pegi.nl();

            "Continuous".toggle("Will keep adding decal every frame while the mouse is down", 80, ref InspectedBrush.decalContentious).nl();

            "Rotation".write("Rotation method", 60);

            InspectedBrush.decalRotationMethod = (DecalRotationMethod)pegi.editEnum(InspectedBrush.decalRotationMethod); 
            pegi.newLine();
            switch (InspectedBrush.decalRotationMethod)
            {
                case DecalRotationMethod.Set:
                    "Angle:".write("Decal rotation", 60);
                    changes |= pegi.edit(ref InspectedBrush.decalAngle, -90, 450);
                    break;
                case DecalRotationMethod.StrokeDirection:
                    "Ang Offset:".edit("Angle modifier after the rotation method is applied", 80, ref InspectedBrush.decalAngleModifier, -180f, 180f);
                    break;
            }

            pegi.newLine();
            if (!BrushExtensions.HasFlag(InspectedBrush.mask, BrushMask.A))
                "! Alpha chanel is disabled. Decals may not render properly".writeHint();

            return changes;

        }
        #endif

    }

    public class BrushTypeLazy : BrushType {
        private static BrushTypeLazy _inst;
        public BrushTypeLazy() { _inst = this; }
        public static BrushTypeLazy Inst { get { InitIfNull(); return _inst; } }

        public override bool StartPaintingTheMomentMouseIsDown => false; 
        protected override string ShaderKeyword(bool texcoord2) => "BRUSH_2D"; 

        public override string NameForDisplayPEGI => "Lazy";
        
        private float _lazySpeedDynamic = 1;
        private float _lazyAngleSmoothed = 1;
        public Vector2 previousDirectionLazy;

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
            
            if (TexMGMT.bigRtPair == null) TexMGMT.UpdateBuffersState();

            var meshWidth = br.StrokeWidth(id.width, false); 
            
            var tf = RtBrush;

            var direction = st.DeltaUv;

            var isTail = st.firstStroke;

            if (!isTail && !smooth)
            {
                var st2 = new StrokeVector(st)
                {
                    firstStroke = false
                };
                r.Shader_UpdateStrokeSegment(br, br.speed * 0.05f, id, st2, painter);

                Vector3 junkPoint = st.uvFrom + st.previousDelta * 0.01f;
                BrushMesh = PainterCamera.BrushMeshGenerator.GetStreak(UvToPosition(st.uvFrom), UvToPosition(junkPoint), meshWidth, true, false);
                tf.localScale = Vector3.one;
                tf.localRotation = Quaternion.identity;
                tf.localPosition = new Vector3(0, 0, 10);


                r.Render();
                st.uvFrom = junkPoint;
                isTail = true;
            }

            r.Shader_UpdateStrokeSegment(br, br.speed * 0.05f, id, st, painter);

            BrushMesh = PainterCamera.BrushMeshGenerator.GetStreak(UvToPosition(st.uvFrom), UvToPosition(st.uvTo), meshWidth, st.mouseUp, isTail);
            tf.localScale = Vector3.one;
            tf.localRotation = Quaternion.identity;
            tf.localPosition = new Vector3(0, 0, 10);

            st.previousDelta = direction;

            r.Render();

            AfterStroke(painter, br, st);
        }
    }

    public class BrushTypeSphere : BrushType {

        static BrushTypeSphere _inst;

        public BrushTypeSphere() { _inst = this; }

        public static BrushTypeSphere Inst { get { InitIfNull(); return _inst; } }

        protected override string ShaderKeyword(bool texcoord2) => texcoord2 ? "BRUSH_3D_TEXCOORD2" : "BRUSH_3D"; 

        public override bool IsA3DBrush => true; 

        public override bool SupportedForTerrainRt => false; 

        public override bool NeedsGrid => Cfg.useGridForBrush; 

        public override string NameForDisplayPEGI => "Sphere";
        
        static void PrepareSphereBrush(ImageMeta id, BrushConfig br, StrokeVector stroke, PlaytimePainter pntr)
        {
            if (TexMGMT.bigRtPair.IsNullOrEmpty())
                TexMGMT.UpdateBuffersState();

            if (stroke.mouseDwn)
                stroke.posFrom = stroke.posTo;

            TexMGMT.Shader_UpdateStrokeSegment(br, br.speed * 0.05f, id, stroke, pntr);

            Vector2 offset = id.offset - stroke.unRepeatedUv.Floor();

            stroke.SetWorldPosInShader();

            PainterDataAndConfig.BRUSH_EDITED_UV_OFFSET.GlobalValue = new Vector4(id.tiling.x, id.tiling.y, offset.x, offset.y);
            PainterDataAndConfig.BRUSH_ATLAS_SECTION_AND_ROWS.GlobalValue = new Vector4(0, 0, 1, 0);
        }

        public override void PaintRenderTexture(PlaytimePainter painter, BrushConfig br, StrokeVector st)
        {

            ImageMeta id = painter.ImgMeta;

            BeforeStroke(painter, br, st);

            PrepareSphereBrush(id, br, st, painter);

            if (!st.mouseDwn)
            {
                TexMGMT.brushRenderer.UseMeshAsBrush(painter);
                TexMGMT.Render();
            }

            AfterStroke(painter, br, st);
        }

        public static void Paint(RenderTexture rt, GameObject go, SkinnedMeshRenderer skinner, BrushConfig br, StrokeVector st, int submeshIndex)
        {
            br.BlitMode.PrePaint(null, br, st);
            PrepareSphereBrush(rt.GetImgData(), br, st, null);
            TexMGMT.brushRenderer.UseSkinMeshAsBrush(go, skinner, submeshIndex);
            TexMGMT.Render();
            AfterStroke(br);
        }

        public static void Paint(RenderTexture rt, GameObject go, Mesh mesh, BrushConfig br, StrokeVector st, List<int> subMeshIndex)
        {
            br.BlitMode.PrePaint(null, br, st);
            PrepareSphereBrush(rt.GetImgData(), br, st, null);
            TexMGMT.brushRenderer.UseMeshAsBrush(go, mesh, subMeshIndex);
            TexMGMT.Render();
            AfterStroke(br);
        }

        public static void PaintAtlased(RenderTexture rt, GameObject go, Mesh mesh, BrushConfig br, StrokeVector st, List<int> subMeshIndex, int aTexturesInRow)
        {

            br.BlitMode.PrePaint(null, br, st);

            PainterDataAndConfig.BRUSH_ATLAS_SECTION_AND_ROWS.GlobalValue = new Vector4(0, 0, aTexturesInRow, 1);

            PrepareSphereBrush(rt.GetImgData(), br, st, null);
            TexMGMT.brushRenderer.UseMeshAsBrush(go, mesh, subMeshIndex);
            TexMGMT.Render();
            AfterStroke(br);

            PainterDataAndConfig.BRUSH_ATLAS_SECTION_AND_ROWS.GlobalValue = new Vector4(0, 0, 1, 0);
        }
      
        #region Inspector
        #if PEGI
        public override bool Inspect()
        {
            bool changed = "Paint On Grid".toggleIcon(ref Cfg.useGridForBrush, true); 

            changed |= base.Inspect();

            return changed;
        }
        #endif
        #endregion
    }
}