using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using PlaytimePainter.CameraModules;
using QuizCanners.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PlaytimePainter
{

#pragma warning disable IDE0019 // Use pattern matching

    public class BrushTypes
    {

        public abstract class Base : PainterClass, IEditorDropdown, IPEGI, IGotDisplayName
        {

            public Base AsBase => this;

            private static List<Base> _allTypes;

            public static List<Base> AllTypes
            {
                get
                {
                    InitIfNull();
                    return _allTypes;
                }
            }

            protected static void InitIfNull()
            {
                if (_allTypes != null) return;

                _allTypes = new List<Base>
                {
                    new Normal(),
                    new Decal(),
                    new Lazy(),
                    new Sphere(),
                    new Pixel(),
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

            protected static Vector2 UvToPosition(Vector2 uv)
            {
                return (uv - Vector2.one * 0.5f) * PainterCamera.OrthographicSize * 2;
            }

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

                QcUnity.SetShaderKeyword(ShaderKeyword(texcoord2), true);

            }

            protected virtual string ShaderKeyword(bool texcoord2) => null;

            private static int _typesCount;
            public readonly int index;

            protected Base()
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

            public virtual string NameForDisplayPEGI() => Translation.GetText();

            public virtual string ToolTip => Translation.GetDescription(); //NameForDisplayPEGI()+ " (No Tooltip)";

            protected virtual MsgPainter Translation => MsgPainter.Unnamed;

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
                    var blitMode = br.GetBlitMode(br.IsCpu(p.TexMeta));
                    if (blitMode.NeedsWorldSpacePosition && !IsAWorldSpaceBrush)
                        return false;

                }

                return

                    (id.target == TexTarget.Texture2D && SupportedByTex2D) ||
                    (id.target == TexTarget.RenderTexture &&
                     ((SupportedByRenderTexturePair && !id.renderTexture)
                      || (SupportedBySingleBuffer && id.renderTexture)));
            }

            public virtual void Inspect()
            {
                if (Brush.InspectedIsCpuBrush || !PainterCamera.Inst)
                    return;

                var changed = false;

                var br = InspectedBrush;

                var adv = InspectAdvanced;

               // var p = InspectedPainter;

                if (Brush.showAdvanced || InspectedBrush.useMask)
                {

                    if (adv || br.useMask)
                        "Mask".toggleIcon("Multiply Brush Speed By Mask Texture's alpha", ref br.useMask, true)
                            .changes(ref changed);

                    if (br.useMask)
                    {

                        pegi.selectOrAdd(ref br.selectedSourceMask, ref Cfg.masks).nl(ref changed);

                        if (adv)
                            "Mask greyscale".toggleIcon("Otherwise will use alpha", ref br.maskFromGreyscale)
                                .nl(ref changed);

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

                pegi.nl();

            }

            #endregion
            
            public virtual void OnShaderBrushUpdate(Brush brush)
            {

            }

            public virtual void PaintPixelsInRam(PaintCommand.UV command)
            {
                Brush br = command.Brush;
                Stroke st = command.Stroke;

                var deltaUv = st.uvTo - st.uvFrom;

                if (deltaUv.magnitude > (0.2f + st.avgBrushSpeed * 3))
                    deltaUv = Vector2.zero; // This is made to avoid glitch strokes on seams
                else st.avgBrushSpeed = (st.avgBrushSpeed + deltaUv.magnitude) / 2;

                var alpha = Mathf.Clamp01(br.Flow * (Application.isPlaying ? Time.deltaTime : 0.1f));

                var id = command.TextureData;

                var deltaPos = st.DeltaWorldPos;

                float steps = 1;

                if (id.disableContiniousLine)
                {

                    st.uvFrom = st.uvTo;
                    st.posFrom = st.posTo;

                }
                else
                {

                    var uvDist = (deltaUv.magnitude * id.width * 8 / br.Size(false));
                    var worldDist = st.DeltaWorldPos.magnitude;

                    steps = (int) Mathf.Max(1, IsAWorldSpaceBrush ? worldDist : uvDist);

                    deltaUv /= steps;
                    deltaPos /= steps;

                    st.uvFrom += deltaUv;
                    st.posFrom += deltaPos;
                }

                Action<PaintCommand.UV> blitMethod = null;

                command.strokeAlphaPortion = alpha;

                var painter = command.TryGetPainter();

                foreach (var p in CameraModuleBase.BrushPlugins)
                    if (p.IsEnabledFor(painter, id, br))
                    {
                        p.PaintPixelsInRam(command);//st, alpha, id, br, painter);
                        blitMethod = p.PaintPixelsInRam;
                        break;
                    }

                if (blitMethod == null)
                {
                    blitMethod = BlitFunctions.Paint;
                    blitMethod(command);
                }

                for (float i = 1; i < steps; i++)
                {
                    st.uvFrom += deltaUv;
                    st.posFrom += deltaPos;
                    blitMethod(command);
                }

                command.OnStrokeComplete();//.AfterStroke(st);
            }

            public virtual void PaintRenderTextureInWorldSpace(PaintCommand.WorldSpaceBase command) { }

            public virtual void PaintRenderTextureUvSpace(PaintCommand.UV command) 
            {
                TextureMeta textureMeta = command.TextureData;
                Brush br = command.Brush;
                Stroke st = command.Stroke;

                BeforeStroke(command); 

                if (st.CrossedASeam())
                    st.uvFrom = st.uvTo;

                command.strokeAlphaPortion = Mathf.Clamp01(br.Flow * 0.05f);

                TexMGMT.SHADER_STROKE_SEGMENT_UPDATE(command); 

                var rb = RtBrush;

                rb.localScale = Vector3.one;
                var direction = st.DeltaUv;
                var length = direction.magnitude;
                BrushMesh = PainterCamera.BrushMeshGenerator.GetLongMesh(length * 256, br.StrokeWidth(textureMeta.width, false));
                rb.localRotation = Quaternion.Euler(new Vector3(0, 0,
                    (direction.x > 0 ? -1 : 1) * Vector2.Angle(Vector2.up, direction)));

                rb.localPosition = Stroke.BrushWorldPositionFrom((st.uvFrom + st.uvTo) * 0.5f);

                TexMGMT.Render();
                
                AfterStroke(command);

            }
            
            public void BeforeStroke(PaintCommand.UV command)//Brush br, Stroke st, PlaytimePainter painter = null)
            {

               // Brush br = command.Brush;
              //  Stroke st = command.Stroke;
                PaintCommand.ForPainterComponent painterCommand = command as PaintCommand.ForPainterComponent;

                if (!RenderTextureBuffersManager.secondBufferUpdated)
                    RenderTextureBuffersManager.UpdateBufferTwo();

                if (painterCommand!= null)
                    foreach (var p in painterCommand.painter.Modules)
                        p.BeforeGpuStroke(painterCommand);//br, st, this);
            }

            public virtual void AfterStroke(PaintCommand.UV command)
            {
                PaintCommand.ForPainterComponent painterCommand = command as PaintCommand.ForPainterComponent;
                Brush br = command.Brush;
                Stroke st = command.Stroke;
                TextureMeta id = command.TextureData;

                command.OnStrokeComplete();

                if (br.useMask && st.MouseUpEvent && br.randomMaskOffset)
                    br.maskOffset = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));

                if (command.usedAlphaBuffer)
                {
                    var sh = br.GetBlitMode(false).ShaderForAlphaBufferBlit;
                    if (painterCommand==null || painterCommand.painter.NotUsingPreview)
                        TexMGMT.UpdateFromAlphaBuffer(id.CurrentRenderTexture(), sh);
                    else
                        TexMGMT.AlphaBufferSetDirtyBeforeRender(id, sh);
                }
                else if (!br.IsSingleBufferBrush() && !command.Is3DBrush)
                    TexMGMT.UpdateBufferSegment();

                if (painterCommand!= null)
                    foreach (var p in painterCommand.painter.Modules)
                        p.AfterGpuStroke(painterCommand);
            }
        }

        public class Pixel : Base
        {
            private static Pixel _inst;

            public Pixel()
            {
                _inst = this;
            }

            public static Pixel Inst
            {
                get
                {
                    InitIfNull();
                    return _inst;
                }
            }

            protected override string ShaderKeyword(bool texcoord2) => "BRUSH_SQUARE";

            public override bool SupportedByTex2D => true;

            protected override MsgPainter Translation => MsgPainter.BrushTypePixel;

            public override bool IsPixelPerfect => true;

            public override void PaintRenderTextureUvSpace(PaintCommand.UV command)
            {
                Brush br = command.Brush;
                Stroke st = command.Stroke;
                TextureMeta id = command.TextureData;
                BeforeStroke(command);

                if (st.CrossedASeam())
                    st.uvFrom = st.uvTo;
                
                command.strokeAlphaPortion = Mathf.Clamp01(br.Flow * 0.05f);

                TexMGMT.SHADER_STROKE_SEGMENT_UPDATE(command);// br, br.Speed * 0.05f, id, st, out alphaBuffer, painter);

                RtBrush.localScale = Vector3.one * br.StrokeWidth(id.width, false);

                BrushMesh = PainterCamera.BrushMeshGenerator.GetQuad();
                RtBrush.localRotation = Quaternion.identity;

                RtBrush.localPosition = st.BrushWorldPosition;

                TexMGMT.Render();

                AfterStroke(command);//painter, br, st, alphaBuffer, id);
            }
        }

        public class Normal : Base
        {
            private static Normal _inst;

            public Normal()
            {
                _inst = this;
            }

            public static Normal Inst
            {
                get
                {
                    InitIfNull();
                    return _inst;
                }
            }

            protected override string ShaderKeyword(bool texcoord) => "BRUSH_2D";

            public override bool SupportedByTex2D => true;

            protected override MsgPainter Translation => MsgPainter.BrushTypeNormal;
            
            public static void Paint(Vector2 uv, Brush br, RenderTexture rt) {

                var command = new PaintCommand.UV(new Stroke(uv)
                {
                    firstStroke = false
                }, rt.GetTextureMeta(), br)
                {
                    strokeAlphaPortion = Mathf.Clamp01(br.Flow * 0.05f)
                };

                TexMGMT.SHADER_STROKE_SEGMENT_UPDATE(command); // br, br.Speed * 0.05f, id, stroke, out alphaBuffer);

                float width = br.StrokeWidth(command.TextureData.width, false);

                RtBrush.localScale = Vector3.one;

                BrushMesh = PainterCamera.BrushMeshGenerator.GetLongMesh(0, width);
                RtBrush.localRotation = Quaternion.Euler(new Vector3(0, 0, Vector2.Angle(Vector2.up, Vector2.zero)));

                RtBrush.localPosition = Stroke.BrushWorldPositionFrom(command.Stroke.uvTo);

                TexMGMT.Render();

                br.GetBrushType(false).AfterStroke(command); 

            }

            public static void Paint(RenderTexture renderTexture, Brush br, Stroke st) =>
                _inst.PaintRenderTextureUvSpace(new PaintCommand.UV(st, renderTexture, br));

        }

        public class Decal : Base
        {

            public enum RotationMethod { Constant, Random, FaceStrokeDirection }
            
            private static Decal _inst;

            public Decal()
            {
                _inst = this;
            }

            public static Decal Inst
            {
                get
                {
                    InitIfNull();
                    return _inst;
                }
            }

            public override bool SupportedBySingleBuffer => false;

            public override bool SupportedForTerrainRt => false;

            public override bool SupportsAlphaBufferPainting => false;

            protected override string ShaderKeyword(bool texcoord) => "BRUSH_DECAL";
            public override bool IsUsingDecals => true;

            private Vector2 _previousUv;


            private readonly ShaderProperty.TextureValue _decalHeightProperty =
                new ShaderProperty.TextureValue("_VolDecalHeight");

            private readonly ShaderProperty.TextureValue _decalOverlayProperty =
                new ShaderProperty.TextureValue("_VolDecalOverlay");

            private readonly ShaderProperty.VectorValue _decalParametersProperty =
                new ShaderProperty.VectorValue("_DecalParameters");


            public override void OnShaderBrushUpdate(Brush brush)
            {
                var vd = Cfg.decals.TryGet(brush.selectedDecal);

                if (vd == null)
                    return;

                _decalHeightProperty.GlobalValue = vd.heightMap;
                _decalOverlayProperty.GlobalValue = vd.overlay;
                _decalParametersProperty.GlobalValue = new Vector4(
                    brush.decalAngle * Mathf.Deg2Rad,
                    (vd.type == VolumetricDecalType.Add) ? 1 : -1,
                    Mathf.Clamp01(brush.Flow / 10f),
                    0);
            }

            public override void PaintRenderTextureUvSpace( PaintCommand.UV command) //PlaytimePainter painter, Brush br, Stroke st)
            {
                
                Brush br = command.Brush;
                Stroke st = command.Stroke;

                BeforeStroke(command);

                var id = command.TextureData; 

                if (st.firstStroke || br.decalContentious)
                {

                    if (br.rotationMethod == RotationMethod.FaceStrokeDirection)
                    {
                        var delta = st.uvTo - _previousUv;

                        var portion = Mathf.Clamp01(delta.magnitude * id.width * 4 / br.Size(false));

                        var newAngle = Vector2.SignedAngle(Vector2.up, delta) + br.decalAngleModifier;
                        br.decalAngle = Mathf.LerpAngle(br.decalAngle, newAngle, portion);

                        _previousUv = st.uvTo;

                    }

                    command.strokeAlphaPortion = 1;

                    TexMGMT.SHADER_STROKE_SEGMENT_UPDATE(command); // br, 1, id, st, out alphaBuffer, command.painter);
                    var tf = RtBrush;
                    tf.localScale = Vector3.one * br.Size(false);
                    tf.localRotation = Quaternion.Euler(new Vector3(0, 0, br.decalAngle));
                    BrushMesh = PainterCamera.BrushMeshGenerator.GetQuad();

                    st.uvTo = st.uvTo.To01Space();

                    var deltaUv = st.DeltaUv;

                    var uv = st.uvTo;

                    if (br.rotationMethod == RotationMethod.FaceStrokeDirection && !st.firstStroke)
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

                    tf.localPosition = Stroke.BrushWorldPositionFrom(uv);

                    TexMGMT.Render();

                    AfterStroke(command);

                }
                else
                    command.OnStrokeComplete(); //painter.AfterStroke(st);
            }

            public override void AfterStroke(PaintCommand.UV command)
            {
                Brush br = command.Brush;

                base.AfterStroke(command);

                if (br.rotationMethod != RotationMethod.Random) return;

                br.decalAngle = Random.Range(-90f, 450f);
                OnShaderBrushUpdate(Cfg.Brush);
            }

            #region Inspector

            protected override MsgPainter Translation => MsgPainter.BrushTypeDecal;
            
           public override void Inspect()
            {

                pegi.select_Index(ref InspectedBrush.selectedDecal, Cfg.decals);

                var decal = Cfg.decals.TryGet(InspectedBrush.selectedDecal);

                if (decal == null)
                {
                    pegi.FullWindow.WarningDocumentationClickOpen("Select a valid decal. You can add some in Config -> Lists.","No Decal selected");
                }

                pegi.nl();
                
                "Continuous".toggle("Will keep adding decal every frame while the mouse is down", 80,
                    ref InspectedBrush.decalContentious);

                pegi.FullWindow.DocumentationClickOpen("Continious Decal will keep painting every frame while mouse button is held", "Countinious Decal");

                pegi.nl();

                "Rotation".write("Rotation method", 60);

                pegi.editEnum(ref InspectedBrush.rotationMethod).nl();

                switch (InspectedBrush.rotationMethod)
                {
                    case RotationMethod.Constant:
                        "Angle:".write("Decal rotation", 60);
                        pegi.edit(ref InspectedBrush.decalAngle, -90, 450);
                        break;
                    case RotationMethod.FaceStrokeDirection:
                        "Ang Offset:".edit("Angle modifier after the rotation method is applied", 80,
                            ref InspectedBrush.decalAngleModifier, -180f, 180f);
                        break;
                }

                pegi.nl();
                if (!InspectedBrush.mask.HasFlag(ColorMask.A))
                    "! Alpha chanel is disabled. Decals may not render properly".writeHint();

            }

            #endregion
        }

        public enum VolumetricDecalType
        {
            Add,
            Dent
        }

        [Serializable]
        public class VolumetricDecal : IEditorDropdown, IPEGI, IGotName, IGotDisplayName
        {
            public string decalName;
            public VolumetricDecalType type;
            public Texture2D heightMap;
            public Texture2D overlay;

            #region Inspector
            
            public bool ShowInDropdown() => heightMap && overlay;

            public string NameForPEGI
            {
                get { return decalName; }
                set { decalName = value; }
            }

            public void Inspect()
            {
                var changed = this.inspect_Name().nl();

                "GetBrushType".editEnum(40, ref type).nl(ref changed);
                "Height Map".edit(ref heightMap).nl(ref changed);
                "Overlay".edit(ref overlay).nl(ref changed);
            }

            public string NameForDisplayPEGI() => "{0} ({1})".F(decalName, type);

            #endregion
        }

        public class Lazy : Base
        {
            private static Lazy _inst;

            public Lazy()
            {
                _inst = this;
            }

            public static Lazy Inst
            {
                get
                {
                    InitIfNull();
                    return _inst;
                }
            }

            public override bool StartPaintingTheMomentMouseIsDown => false;
            protected override string ShaderKeyword(bool texcoord2) => "BRUSH_2D";

            private float _lazySpeedDynamic = 1;
            private float _lazyAngleSmoothed = 1;
            public Vector2 previousDirectionLazy;

            protected override MsgPainter Translation => MsgPainter.BrushTypeLazy;

            public override void PaintRenderTextureUvSpace(PaintCommand.UV command)//PlaytimePainter painter, Brush br, Stroke st)
            {

                Brush br = command.Brush;
                Stroke st = command.Stroke;
                var id = command.TextureData;

                BeforeStroke(command);

                var deltaUv = st.DeltaUv; //uv - st.uvFrom;//.Previous_uv;
                var magnitude = deltaUv.magnitude;
                
                var width = br.Size(false) / id.width * 4;

                var trackPortion = (deltaUv.magnitude - width * 0.5f) * 0.25f;

                if (!(trackPortion > 0) && !st.MouseUpEvent) return;

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
                    st.MouseUpEvent = true;
                    st.uvTo = st.uvFrom; // painter.Previous_uv;
                    deltaUv = Vector2.zero;
                    smooth = false;
                }

                previousDirectionLazy = deltaUv;

                if (!st.MouseUpEvent)
                {
                    if (smooth)
                    {
                        var clockwise = Vector3.Cross(st.previousDelta, deltaUv).z > 0 ? 1f : -1f;
                        var sin = Mathf.Sin(angle) * clockwise;
                        float maxSinus = 8;
                        _lazyAngleSmoothed = Mathf.Abs(_lazyAngleSmoothed) > Mathf.Abs(sin)
                            ? sin
                            : Mathf.Lerp(_lazyAngleSmoothed, sin, 0.2f);

                        sin = _lazyAngleSmoothed;

                        if ((sin * sin > maxSinus * maxSinus) || ((sin > 0) != (maxSinus > 0)))
                        {
                            var absSin = Mathf.Abs(sin);
                            var absNSin = Mathf.Abs(maxSinus);

                            if (absSin < absNSin) maxSinus = maxSinus * absSin / absNSin;

                            st.uvTo = st.uvFrom + st.previousDelta.normalized.Rotate_Radians(maxSinus * clockwise) *
                                      trackPortion;
                            _lazySpeedDynamic = trackPortion;
                        }
                        else
                        {
                            _lazySpeedDynamic = Mathf.Min(deltaUv.magnitude * 0.5f,
                                Mathf.Lerp(_lazySpeedDynamic, deltaUv.magnitude * 0.5f, 0.001f));

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

                var meshWidth = br.StrokeWidth(id.width, false);

                var tf = RtBrush;

                var direction = st.DeltaUv;

                var isTail = st.firstStroke;

               // bool alphaBuffer;

                if (!isTail && !smooth)
                {
                    /*var st2 = new Stroke(st)
                    {
                        firstStroke = false
                    };*/
                    r.SHADER_STROKE_SEGMENT_UPDATE(command);//br, br.Speed * 0.05f, id, st2, out alphaBuffer, painter);

                    Vector3 junkPoint = st.uvFrom + st.previousDelta * 0.01f;
                    BrushMesh = PainterCamera.BrushMeshGenerator.GetStreak(UvToPosition(st.uvFrom),
                        UvToPosition(junkPoint), meshWidth, true, false);
                    tf.localScale = Vector3.one;
                    tf.localRotation = Quaternion.identity;
                    tf.localPosition = new Vector3(0, 0, 10);


                    r.Render();
                    st.uvFrom = junkPoint;
                    isTail = true;
                }

                command.strokeAlphaPortion = Mathf.Clamp01(br.Flow * 0.05f);

                r.SHADER_STROKE_SEGMENT_UPDATE(command);//br, br.Speed * 0.05f, id, st, out alphaBuffer, painter);

                BrushMesh = PainterCamera.BrushMeshGenerator.GetStreak(UvToPosition(st.uvFrom), UvToPosition(st.uvTo),
                    meshWidth, st.MouseUpEvent, isTail);
                tf.localScale = Vector3.one;
                tf.localRotation = Quaternion.identity;
                tf.localPosition = new Vector3(0, 0, 10);

                st.previousDelta = direction;

                r.Render();

                AfterStroke(command); //painter, br, st, alphaBuffer, id);
            }
        }

        public class Sphere : Base
        {
            private static Sphere _inst;

            public Sphere()
            {
                _inst = this;
            }

            public static Sphere Inst
            {
                get
                {
                    InitIfNull();
                    return _inst;
                }
            }

            protected override string ShaderKeyword(bool texcoord2) => texcoord2 ? "BRUSH_3D_TEXCOORD2" : "BRUSH_3D";

            public override bool IsAWorldSpaceBrush => true;

            public override bool SupportsAlphaBufferPainting => true;

            public override bool SupportedForTerrainRt => false;

            public override bool NeedsGrid => Cfg.useGridForBrush;

            private static void PrepareSphereBrush(PaintCommand.WorldSpaceBase command)
            {
                Brush br = command.Brush;
                var td = command.TextureData;
                // if (stroke.mouseDwn)
                //    stroke.posFrom = stroke.posTo;

                command.strokeAlphaPortion = Mathf.Clamp01(br.Flow * 0.05f);

                TexMGMT.SHADER_STROKE_SEGMENT_UPDATE(command); // br, br.Speed * 0.05f, id, stroke, out alphaBuffer, painter);

                var offset = command.TextureData.offset - command.Stroke.unRepeatedUv.Floor();

                command.Stroke.SetWorldPosInShader();

                PainterShaderVariables.BRUSH_EDITED_UV_OFFSET.GlobalValue =
                    new Vector4(td.tiling.x, td.tiling.y, offset.x, offset.y);
                PainterShaderVariables.BRUSH_ATLAS_SECTION_AND_ROWS.GlobalValue = new Vector4(0, 0, 1, 0);
            }

            public override void PaintRenderTextureInWorldSpace(PaintCommand.WorldSpaceBase command) //PlaytimePainter painter, Brush br, Stroke st)
            {

               // var id = command.TextureData;//painter.TexMeta;

                BeforeStroke(command);//br, st, painter);

                //bool alphaBuffer;

                PrepareSphereBrush(command); //id, br, st, out alphaBuffer, painter);

                if (!command.Stroke.MouseDownEvent)
                {
                    TexMGMT.brushRenderer.Prepare(command);
                    TexMGMT.Render();
                }

                AfterStroke(command); //painter, br, st, alphaBuffer, id);
            }

            public static void Paint(PaintCommand.WorldSpaceBase command)
            {

                Brush br = command.Brush;
               // Stroke st = command.Stroke; 


                br.GetBlitMode(false).PrePaint(command);

                PrepareSphereBrush(command); 
                TexMGMT.brushRenderer.Prepare(command);
                TexMGMT.Render();
                br.GetBrushType(false).AfterStroke(command);
            }


            public static void PaintAtlased(PaintCommand.WorldSpaceBase command,int aTexturesInRow)
            {
                PainterShaderVariables.BRUSH_ATLAS_SECTION_AND_ROWS.GlobalValue = new Vector4(0, 0, aTexturesInRow, 1);

                Paint(command);

                PainterShaderVariables.BRUSH_ATLAS_SECTION_AND_ROWS.GlobalValue = new Vector4(0, 0, 1, 0);
            }

            #region Inspector

            protected override MsgPainter Translation => MsgPainter.BrushTypeSphere;

           public override void Inspect()
            {

                var changed = false;

                var br = InspectedBrush;

                bool suggestGrid = false;

                if (InspectedPainter && !InspectedPainter.GetMesh())
                {
                    if (!Cfg.useGridForBrush)
                    {
                        "No mesh for sphere painting detected.".writeWarning();
                        suggestGrid = true;
                    }
                }

                if (InspectAdvanced || Cfg.useGridForBrush || suggestGrid)
                {
                    (Cfg.useGridForBrush
                        ? ("Grid: Z, X - change plane  |  Ctrl+LMB - reposition GRID")
                        : "Paint On Grid").toggleIcon(ref Cfg.useGridForBrush).nl();

                    pegi.line();
                    pegi.nl();
                }

                if (!br.useAlphaBuffer && (br.worldSpaceBrushPixelJitter || InspectAdvanced))
                {
                    "One Pixel Jitter".toggleIcon(ref br.worldSpaceBrushPixelJitter).changes(ref changed);
                    pegi.FullWindow.DocumentationClickOpen("Will provide a single pixel jitter which can help fix seams not being painted properly", "Why use one pixel jitter?");
                    pegi.nl();
                }

                base.Inspect();

                pegi.nl();
            }

            #endregion
        }
    }
}