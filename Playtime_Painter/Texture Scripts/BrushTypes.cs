using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter
{

    public static class BlitModeExtensions {
        public static void KeywordSet(string name, bool to)
        {
            if (to)
                Shader.EnableKeyword(name);
            else
                Shader.DisableKeyword(name);
        }

        public static void SetShaderToggle(bool value, string iftrue, string iffalse)
        {
            Shader.DisableKeyword(value ? iffalse : iftrue);
            Shader.EnableKeyword(value ? iftrue : iffalse);
        }
    }

    public abstract class BrushType : PainterStuff, IEditorDropdown, IPEGI
    {

        public static Blit_Functions.PaintTexture2DMethod tex2DPaintPlugins;

        private static List<BrushType> _allTypes;

        public static List<BrushType> AllTypes { get { InitIfNull(); return _allTypes; } }

        public static void InitIfNull()
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

        public Vector2 UvToPosition(Vector2 uv) { return (uv - Vector2.one * 0.5f) * PainterCamera.orthoSize * 2; }

        public Vector2 To01space(Vector2 from)
        {
            from.x %= 1;
            from.y %= 1;
            if (from.x < 0) from.x += 1;
            if (from.y < 0) from.y += 1;
            return from;
        }

        public void SetKeyword(bool texcoord2)
        {

            foreach (BrushType bs in AllTypes)
            {
                string name = bs.ShaderKeyword(true);
                if (name != null)
                    Shader.DisableKeyword(name);

                name = bs.ShaderKeyword(false);
                if (name != null)
                    Shader.DisableKeyword(name);
            }

            if (ShaderKeyword(texcoord2) != null)
                BlitModeExtensions.KeywordSet(ShaderKeyword(texcoord2), true);

        }

        protected virtual string ShaderKeyword(bool texcoord2) { return null; }

        static int typesCount = 0;
        public int index;

        public BrushType()
        {
            index = typesCount;
            typesCount++;
        }

        public virtual bool SupportedByTex2D { get { return false; } }
        public virtual bool SupportedByRenderTexturePair { get { return true; } }
        public virtual bool SupportedBySingleBuffer { get { return true; } }
        public virtual bool IsA3DBrush { get { return false; } }
        public virtual bool IsPixelPerfect { get { return false; } }
        public virtual bool IsUsingDecals { get { return false; } }
        public virtual bool StartPaintingTheMomentMouseIsDown { get { return true; } }
        public virtual bool SupportedForTerrain_RT { get { return true; } }
        public virtual bool NeedsGrid { get { return false; } }

        #if PEGI
        public virtual bool ShowInDropdown()
        {
            if (PlaytimePainter.inspectedPainter == null)
                return false;

            ImageData id = InspectedImageData;

            if (id == null)
                return false;

            return  //((id.destination == dest.Texture2D) && (supportedByTex2D)) || 

                (((id.destination == TexTarget.RenderTexture) &&
                    ((SupportedByRenderTexturePair && (id.renderTexture == null))
                        || (SupportedBySingleBuffer && (id.renderTexture != null)))
                ));
        }

        public virtual bool Inspect()
        {

            bool change = false;
  
            if (BrushConfig.InspectedIsCPUbrush)
                return change;

            if (TexMGMTdata.masks.Count > 0)
            {
                pegi.nl();
                pegi.Space();
                pegi.newLine();

                change |= "Mask".toggleIcon ("Multiply Brush Speed By Mask Texture's alpha", ref InspectedBrush.useMask);

                if (InspectedBrush.useMask) {

                    change |= pegi.selectOrAdd(ref InspectedBrush.selectedSourceMask, ref TexMGMTdata.masks).nl();

                  

                    if (!InspectedBrush.randomMaskOffset)
                        change |= "Mask Offset ".edit(ref InspectedBrush.maskOffset).nl();

                    change |= "Random Mask Offset".toggleIcon(ref InspectedBrush.randomMaskOffset).nl();

          
                    if ("Mask Tiling: ".edit(70, ref InspectedBrush.maskTiling, 1, 8).nl())
                    {
                        InspectedBrush.maskTiling = Mathf.Clamp(InspectedBrush.maskTiling, 0.1f, 64);
                        change = true;
                    }

                    change |= "Flip Mask Alpha".toggleIcon("Alpha = 1-Alpha", ref InspectedBrush.flipMaskAlpha).nl();
                    
                }


            }
    
            if (InspectedPainter.NeedsGrid() && "Center Grid On Object".Click().nl())
                GridNavigator.onGridPos = InspectedPainter.transform.position;

            return change;
        }
#endif
        public virtual void PaintToTexture2D(PlaytimePainter pntr, BrushConfig br, StrokeVector st) {

            Vector2 delta_uv = st.uvTo - st.uvFrom;

            if (delta_uv.magnitude > (0.2f + st.avgBrushSpeed * 3)) delta_uv = Vector2.zero; // This is made to avoid glitch strokes on seams
            else st.avgBrushSpeed = (st.avgBrushSpeed + delta_uv.magnitude) / 2;

            float alpha = Mathf.Clamp01(br.speed * (Application.isPlaying ? Time.deltaTime : 0.1f));

            bool worldSpace = pntr.NeedsGrid();

            var id = pntr.ImgData;

            float uvDist = (delta_uv.magnitude * id.width * 8 / br.Size(false));
            float worldDist = st.Delta_WorldPos.magnitude;

            float steps = (int)Mathf.Max(1, worldSpace ? worldDist : uvDist);

            delta_uv /= steps;
            Vector3 deltaPos = st.Delta_WorldPos / steps;

            st.uvFrom += delta_uv;
            st.posFrom += deltaPos;
            
            Blit_Functions.PaintTexture2DMethod blitMethod = null;

            if (blitMethod == null && tex2DPaintPlugins != null)
                foreach (Blit_Functions.PaintTexture2DMethod p in tex2DPaintPlugins.GetInvocationList())
                    if (p(st, alpha, id, br, pntr)) {
                        blitMethod = p;
                        break;
                    }

            if (blitMethod == null) {
                blitMethod = Blit_Functions.Paint;
                blitMethod(st, alpha, id, br, pntr);
            }

            for (float i = 1; i < steps; i++) {
                st.uvFrom += delta_uv;
                st.posFrom += deltaPos;
                blitMethod(st, alpha, id, br, pntr);
            }

            pntr.AfterStroke(st);
        }

        public virtual void PaintRenderTexture(PlaytimePainter pntr, BrushConfig br, StrokeVector st)
        {

            BeforeStroke(pntr, br, st);

            if (st.CrossedASeam())
                st.uvFrom = st.uvTo;

            ImageData id = pntr.ImgData;

            TexMGMT.Shader_UpdateStrokeSegment(br, br.speed * 0.05f, id, st, pntr);

            var rb = Rtbrush;

            rb.localScale = Vector3.one;
            Vector2 direction = st.Delta_uv;
            float length = direction.magnitude;
            BrushMesh = brushMeshGenerator.inst().GetLongMesh(length * 256, br.StrokeWidth(id.width, false));
            rb.localRotation = Quaternion.Euler(new Vector3(0, 0, (direction.x > 0 ? -1 : 1) * Vector2.Angle(Vector2.up, direction)));

            rb.localPosition = StrokeVector.BrushWorldPositionFrom((st.uvFrom + st.uvTo) / 2);

            TexMGMT.Render();

            AfterStroke(pntr, br, st);
        }

        protected static void AfterStroke(BrushConfig br)
        {

            if ((br.useMask) && (br.randomMaskOffset))
                br.maskOffset = new Vector2(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));

        }

        public virtual void BeforeStroke(PlaytimePainter pntr, BrushConfig br, StrokeVector st)
        {
            foreach (var p in pntr.Plugins)
                p.BeforeGPUStroke(pntr, br, st, this);
        }

        public virtual void AfterStroke(PlaytimePainter pntr, BrushConfig br, StrokeVector st)
        {

            pntr.AfterStroke(st);

            if (!br.IsSingleBufferBrush() && !br.IsA3Dbrush(pntr))
                TexMGMT.UpdateBufferSegment();

            if ((br.useMask) && (st.mouseUp) && (br.randomMaskOffset))
                br.maskOffset = new Vector2(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));

            foreach (var p in pntr.Plugins)
                p.AfterGPUStroke(pntr, br, st, this);

        }

    }

    public class BrushTypePixel : BrushType
    {
        static BrushTypePixel _inst;
        public BrushTypePixel() { _inst = this; }
        public static BrushTypePixel Inst { get { InitIfNull(); return _inst; } }

        protected override string ShaderKeyword(bool texcoord2) { return "BRUSH_SQUARE"; }

        public override bool SupportedByTex2D { get { return true; } }

        public override string ToString() { return "Pixel"; }

        public override bool IsPixelPerfect { get { return true; } }

        public override void PaintRenderTexture(PlaytimePainter pntr, BrushConfig br, StrokeVector st)
        {

          

           BeforeStroke(pntr, br, st);

             if (st.CrossedASeam())
                 st.uvFrom = st.uvTo;

             if (TexMGMT.BigRT_pair == null) TexMGMT.UpdateBuffersState();

             ImageData id = pntr.ImgData;

             TexMGMT.Shader_UpdateStrokeSegment(br, br.speed * 0.05f, id, st, pntr);

             Rtbrush.localScale = Vector3.one * br.StrokeWidth(id.width, false);

             BrushMesh = brushMeshGenerator.inst().GetQuad();
             Rtbrush.localRotation = Quaternion.identity;

             Rtbrush.localPosition = st.BrushWorldPosition;

             TexMGMT.Render();

             AfterStroke(pntr, br, st);
        }

    }

    public class BrushTypeNormal : BrushType {

        static BrushTypeNormal _inst;
        public BrushTypeNormal() { _inst = this; }
        public static BrushTypeNormal Inst { get { InitIfNull(); return _inst; } }

        protected override string ShaderKeyword(bool texcoord) { return "BRUSH_2D"; }

        public override bool SupportedByTex2D { get { return true; } }

        public override string ToString()
        {
            return "Normal";
        }

        public static void Paint(Vector2 uv, BrushConfig br, RenderTexture rt)
        {

            if (TexMGMT.BigRT_pair == null) TexMGMT.UpdateBuffersState();

            var id = rt.GetImgData();
            var stroke = new StrokeVector(uv) {
                firstStroke = false
            };
            TexMGMT.Shader_UpdateStrokeSegment(br, br.speed * 0.05f, id, stroke, null);

            float width = br.StrokeWidth(id.width, false);

            Rtbrush.localScale = Vector3.one;

            BrushMesh = brushMeshGenerator.inst().GetLongMesh(0, width);
            Rtbrush.localRotation = Quaternion.Euler(new Vector3(0, 0, Vector2.Angle(Vector2.up, Vector2.zero)));

            Rtbrush.localPosition = StrokeVector.BrushWorldPositionFrom(uv);

            TexMGMT.Render();

            AfterStroke(br);



        }
    }

    public class BrushTypeDecal : BrushType {

        static BrushTypeDecal _inst;
        public BrushTypeDecal() { _inst = this; }
        public static BrushTypeDecal Inst { get { InitIfNull(); return _inst; } }
        public override bool SupportedBySingleBuffer { get { return false; } }
        protected override string ShaderKeyword(bool texcoord) { return "BRUSH_DECAL"; }
        public override bool IsUsingDecals { get { return true; } }

        public override string ToString()
        {
            return "Decal";
        }

        Vector2 previousUV;

        public override void PaintRenderTexture(PlaytimePainter pntr, BrushConfig br, StrokeVector st)
        {

            BeforeStroke(pntr, br, st);

            ImageData id = pntr.ImgData;

            if ((st.firstStroke) || (br.decalContinious))
            {

                if (br.decalRotationMethod == DecalRotationMethod.StrokeDirection)
                {
                    Vector2 delta = st.uvTo - previousUV;

                    // if ((st.firstStroke) || (delta.magnitude*id.width > br.Size(false)*0.25f)) {

                    float portion = Mathf.Clamp01(delta.magnitude * id.width * 4 / br.Size(false));

                    float newAngle = Vector2.SignedAngle(Vector2.up, delta) + br.decalAngleModifier;
                    br.decalAngle = Mathf.LerpAngle(br.decalAngle, newAngle, portion);

                    previousUV = st.uvTo;
                    //}

                }

                if (TexMGMT.BigRT_pair == null) TexMGMT.UpdateBuffersState();

                TexMGMT.Shader_UpdateStrokeSegment(br, 1, id, st, pntr);
                Transform tf = Rtbrush;
                tf.localScale = Vector3.one * br.Size(false);
                tf.localRotation = Quaternion.Euler(new Vector3(0, 0, br.decalAngle));
                BrushMesh = brushMeshGenerator.inst().GetQuad();

                st.uvTo = st.uvTo.To01Space();

                Vector2 deltauv = st.Delta_uv;

                /* 

                 int strokes = Mathf.Max(1, (br.decalContinious && (!st.firstStroke)) ? (int)(deltauv.magnitude*id.width/br.Size(false)) : 1);

                 deltauv /=  strokes;

                 for (int i = 0; i < strokes; i++) {
                     st.uvFrom += deltauv;*/

                Vector2 uv = st.uvTo;

                if ((br.decalRotationMethod == DecalRotationMethod.StrokeDirection) && (!st.firstStroke))
                {
                    float length = Mathf.Max(deltauv.magnitude * 2 * id.width / br.Size(false), 1);
                    Vector3 scale = tf.localScale;

                    if ((Mathf.Abs(Mathf.Abs(br.decalAngleModifier) - 90)) < 40)
                        scale.x *= length;
                    else
                        scale.y *= length;

                    tf.localScale = scale;
                    uv -= deltauv * ((length - 1) * 0.5f / length);
                }

                tf.localPosition = StrokeVector.BrushWorldPositionFrom(uv);

                TexMGMT.Render();

                AfterStroke(pntr, br, st);

            }
            else
                pntr.AfterStroke(st);
        }

        public override void AfterStroke(PlaytimePainter pntr, BrushConfig br, StrokeVector st)
        {
            base.AfterStroke(pntr, br, st);

            if (br.decalRotationMethod == DecalRotationMethod.Random)
            {
                br.decalAngle = UnityEngine.Random.Range(-90f, 450f);
                TexMGMT.Shader_UpdateDecal(Cfg.brushConfig); //pntr.Dec//Update_Brush_Parameters_For_Preview_Shader();
            }
        }
#if PEGI
        public override bool Inspect()
        {

            bool changes = false;

            changes |= pegi.select(ref InspectedBrush.selectedDecal, TexMGMTdata.decals).nl();

            var decal = TexMGMTdata.decals.TryGet(InspectedBrush.selectedDecal);

            if (decal == null)
                pegi.write("Select valid decal; Assign to Painter Camera.");
            pegi.newLine();

            "Continious".toggle("Will keep adding decal every frame while the mouse is down", 80, ref InspectedBrush.decalContinious).nl();

            "Rotation".write("Rotation method", 60);

            InspectedBrush.decalRotationMethod = (DecalRotationMethod)pegi.editEnum<DecalRotationMethod>(InspectedBrush.decalRotationMethod); // "Random Angle", 90);
            pegi.newLine();
            if (InspectedBrush.decalRotationMethod == DecalRotationMethod.Set)
            {
                "Angle:".write("Decal rotation", 60);
                changes |= pegi.edit(ref InspectedBrush.decalAngle, -90, 450);
            }
            else if (InspectedBrush.decalRotationMethod == DecalRotationMethod.StrokeDirection)
            {
                "Ang Offset:".edit("Angle modifier after the rotation method is applied", 80, ref InspectedBrush.decalAngleModifier, -180f, 180f);
            }

            pegi.newLine();
            if (!InspectedBrush.mask.GetFlag(BrushMask.A))
                pegi.writeHint("! Alpha chanel is disabled. Decals may not render properly");

            return changes;

        }
#endif

    }

    public class BrushTypeLazy : BrushType {

        static BrushTypeLazy _inst;
        public BrushTypeLazy() { _inst = this; }
        public static BrushTypeLazy Inst { get { InitIfNull(); return _inst; } }

        public override bool StartPaintingTheMomentMouseIsDown { get { return false; } }
        protected override string ShaderKeyword(bool texcoord2) { return "BRUSH_2D"; }

        public override string ToString()
        {
            return "Lazy";
        }

        public float LazySpeedDynamic = 1;
        public float LazyAngleSmoothed = 1;
        Vector2 previousDirectionLazy;

        public override void PaintRenderTexture(PlaytimePainter pntr, BrushConfig br, StrokeVector st)
        {

            BeforeStroke(pntr, br, st);
            //	Vector2 outb = new Vector2(Mathf.Floor(st.uvTo.x), Mathf.Floor(st.uvTo.y));
            //	st.uvTo -= outb;
            //	st.uvFrom -= outb;

            Vector2 delta_uv = st.Delta_uv;//uv - st.uvFrom;//.Previous_uv;
            float magn = delta_uv.magnitude;

            var id = pntr.ImgData;

            float width = br.Size(false) / ((float)id.width) * 4;
            //const float followPortion = 0.5f;
            //float follow = width;

            float trackPortion = (delta_uv.magnitude - width * 0.5f) * 0.25f;

            if ((trackPortion > 0) || (st.mouseUp))
            {


                if (st.firstStroke)
                {
                    previousDirectionLazy = st.previousDelta = delta_uv;
                    LazySpeedDynamic = delta_uv.magnitude;
                    LazyAngleSmoothed = 0;
                    // Debug.Log("First stroke");
                }

                float angle = Mathf.Deg2Rad * Vector2.Angle(st.previousDelta, delta_uv);

                bool smooth = angle < Mathf.PI * 0.5f;

                if ((st.CrossedASeam()) && (magn > previousDirectionLazy.magnitude * 8))
                {
                    // Debug.Log("Crossed a seam");
                    st.mouseUp = true;
                    st.uvTo = st.uvFrom;// painter.Previous_uv;
                    delta_uv = Vector2.zero;
                    smooth = false;
                }

                previousDirectionLazy = delta_uv;



                if (!st.mouseUp)
                {
                    if (smooth)
                    {
                        float clockwise = Vector3.Cross(st.previousDelta, delta_uv).z > 0 ? 1 : -1;
                        float sin = Mathf.Sin(angle) * clockwise;
                        float maxSinus = 8;
                        if (Mathf.Abs(LazyAngleSmoothed) > Mathf.Abs(sin)) LazyAngleSmoothed = sin;
                        else
                            LazyAngleSmoothed = Mathf.Lerp(LazyAngleSmoothed, sin, 0.2f);
                        sin = LazyAngleSmoothed;

                        if ((sin * sin > maxSinus * maxSinus) || ((sin > 0) != (maxSinus > 0)))
                        {

                            float absSin = Mathf.Abs(sin);
                            float absNSin = Mathf.Abs(maxSinus);

                            if (absSin < absNSin) maxSinus = maxSinus * absSin / absNSin;

                            st.uvTo = st.uvFrom + st.previousDelta.normalized.Rotate_Radians(maxSinus * clockwise) * trackPortion;
                            LazySpeedDynamic = trackPortion;
                        }
                        else
                        {
                            LazySpeedDynamic = Mathf.Min(delta_uv.magnitude * 0.5f, Mathf.Lerp(LazySpeedDynamic, delta_uv.magnitude * 0.5f, 0.001f));

                            LazySpeedDynamic = Mathf.Max(trackPortion, LazySpeedDynamic);
                            st.uvTo = st.uvFrom + st.previousDelta.normalized.Rotate_Radians(sin) * LazySpeedDynamic;
                        }
                    }
                    else
                    {
                        LazySpeedDynamic = delta_uv.magnitude;
                        LazyAngleSmoothed = 0;
                        st.uvTo = st.uvFrom + delta_uv.normalized * trackPortion;
                    }
                }
                PainterCamera r = TexMGMT;
                //RenderTexturePainter.inst.RenderLazyBrush(painter.Previous_uv, uv, brush.speed * 0.05f, painter.curImgData, brush, painter.LmouseUP, smooth );
                if (TexMGMT.BigRT_pair == null) TexMGMT.UpdateBuffersState();

                float meshWidth = br.StrokeWidth(id.width, false); //.Size(false) / ((float)id.width) * 2 * rtp.orthoSize;

                Transform tf = Rtbrush;

                Vector2 direction = st.Delta_uv;//uvTo - uvFrom;

                bool isTail = st.firstStroke;//(!previousTo.Equals(uvFrom));

                if ((!isTail) && (!smooth))
                {
                    var st2 = new StrokeVector(st)
                    {
                        firstStroke = false
                    };
                    r.Shader_UpdateStrokeSegment(br, br.speed * 0.05f, id, st2, pntr);

                    Vector3 junkPoint = st.uvFrom + st.previousDelta * 0.01f;
                    BrushMesh = brushMeshGenerator.inst().GetStreak(UvToPosition(st.uvFrom), UvToPosition(junkPoint), meshWidth, true, false);
                    tf.localScale = Vector3.one;
                    tf.localRotation = Quaternion.identity;
                    tf.localPosition = new Vector3(0, 0, 10);


                    r.Render();//Render_UpdateSecondBufferIfUsing(id);
                    st.uvFrom = junkPoint;
                    isTail = true;
                }

                r.Shader_UpdateStrokeSegment(br, br.speed * 0.05f, id, st, pntr);

                BrushMesh = brushMeshGenerator.inst().GetStreak(UvToPosition(st.uvFrom), UvToPosition(st.uvTo), meshWidth, st.mouseUp, isTail);
                tf.localScale = Vector3.one;
                tf.localRotation = Quaternion.identity;
                tf.localPosition = new Vector3(0, 0, 10);

                st.previousDelta = direction;

                r.Render();

                AfterStroke(pntr, br, st);

            }
        }
    }

    public class BrushTypeSphere : BrushType
    {

        static BrushTypeSphere _inst;
        public BrushTypeSphere() { _inst = this; }
        public static BrushTypeSphere Inst { get { InitIfNull(); return _inst; } }

        protected override string ShaderKeyword(bool texcoord2) { return (texcoord2 ? "BRUSH_3D_TEXCOORD2" : "BRUSH_3D"); }

        public override bool IsA3DBrush { get { return true; } }
        public override bool SupportedForTerrain_RT { get { return false; } }
        public override bool NeedsGrid { get { return Cfg.useGridForBrush; } }

        public override string ToString()
        {
            return "Sphere";
        }

        static void PrepareSphereBrush(ImageData id, BrushConfig br, StrokeVector stroke, PlaytimePainter pntr)
        {
            if (TexMGMT.BigRT_pair == null) TexMGMT.UpdateBuffersState();

            if (stroke.mouseDwn)
                stroke.posFrom = stroke.posTo;

            TexMGMT.Shader_UpdateStrokeSegment(br, br.speed * 0.05f, id, stroke, null);

            Vector2 offset = id.offset - stroke.unRepeatedUV.Floor();

            stroke.SetWorldPosInShader();

            Shader.SetGlobalVector(PainterDataAndConfig.BRUSH_EDITED_UV_OFFSET, new Vector4(id.tiling.x, id.tiling.y, offset.x, offset.y));
            Shader.SetGlobalVector(PainterDataAndConfig.BRUSH_ATLAS_SECTION_AND_ROWS, new Vector4(0, 0, 1, 0));
        }

        public override void PaintRenderTexture(PlaytimePainter pntr, BrushConfig br, StrokeVector st)
        {

            ImageData id = pntr.ImgData;

            BeforeStroke(pntr, br, st);

            PrepareSphereBrush(id, br, st, pntr);

            if (!st.mouseDwn)
            {
                TexMGMT.brushRendy.UseMeshAsBrush(pntr);
                TexMGMT.Render();
            }

            AfterStroke(pntr, br, st);


        }

        public static void Paint(RenderTexture rt, GameObject go, SkinnedMeshRenderer skinner, BrushConfig br, StrokeVector st, int submeshIndex)
        {
            br.BlitMode.PrePaint(null, br, st);
            PrepareSphereBrush(rt.GetImgData(), br, st, null);
            TexMGMT.brushRendy.UseSkinMeshAsBrush(go, skinner, submeshIndex);
            TexMGMT.Render();
            AfterStroke(br);
        }

        public static void Paint(RenderTexture rt, GameObject go, Mesh mesh, BrushConfig br, StrokeVector st, List<int> submeshIndex)
        {
            br.BlitMode.PrePaint(null, br, st);

            PrepareSphereBrush(rt.GetImgData(), br, st, null);
            TexMGMT.brushRendy.UseMeshAsBrush(go, mesh, submeshIndex);
            TexMGMT.Render();
            AfterStroke(br);
        }

        public static void PaintAtlased(RenderTexture rt, GameObject go, Mesh mesh, BrushConfig br, StrokeVector st, List<int> submeshIndex, int A_Textures_in_row)
        {

            br.BlitMode.PrePaint(null, br, st);

            Shader.SetGlobalVector(PainterDataAndConfig.BRUSH_ATLAS_SECTION_AND_ROWS, new Vector4(0, 0, A_Textures_in_row, 1));

            PrepareSphereBrush(rt.GetImgData(), br, st, null);
            TexMGMT.brushRendy.UseMeshAsBrush(go, mesh, submeshIndex);
            TexMGMT.Render();
            AfterStroke(br);

            Shader.SetGlobalVector(PainterDataAndConfig.BRUSH_ATLAS_SECTION_AND_ROWS, new Vector4(0, 0, 1, 0));
        }
#if PEGI
        public override bool Inspect()
        {
            bool changed = "Paint On Grid".toggleIcon(ref Cfg.useGridForBrush); 

            changed |= base.Inspect();

            return changed;
        }
#endif
    }
}