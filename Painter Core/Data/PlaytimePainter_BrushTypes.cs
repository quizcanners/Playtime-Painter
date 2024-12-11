using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using PainterTool.CameraModules;
using QuizCanners.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PainterTool
{

#pragma warning disable IDE0019 // Use pattern matching

    public class BrushTypes
    {

        private static List<Base> _allTypes;
        public static List<Base> All
        {
            get
            {
                InitIfNull();
                return _allTypes;
            }
        }

        private static void InitIfNull()
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


        public abstract class Base : PainterClass, IInspectorDropdown, IPEGI
        {

     

            protected static Vector2 UvToPosition(Vector2 uv)
            {
                return 2 * Singleton_PainterCamera.OrthographicSize * (uv - Vector2.one * 0.5f);
            }

            public void SetKeyword(bool texcoord2)
            {

                foreach (var bs in All)
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
            public virtual bool NeedsGrid => false;

            #region Inspect

            public override string ToString() => Translation.GetText();

            public virtual string ToolTip => Translation.GetDescription(); //NameForDisplayPEGI()+ " (No Tooltip)";

            protected virtual MsgPainter Translation => MsgPainter.Unnamed;

            public virtual bool ShowInInspectorDropdown()
            {
                var p = PainterComponent.inspected;

                if (!p)
                    return true;

                var id = InspectedImageMeta;

                if (id == null)
                    return false;

                var br = InspectedBrush;

                if (br != null)
                {
                    var blitMode = br.GetBlitMode(br.GetTarget(p.TexMeta));
                    if (blitMode.NeedsWorldSpacePosition && !IsAWorldSpaceBrush)
                        return false;

                }

                return

                    (id.Target == TexTarget.Texture2D && SupportedByTex2D) ||
                    (id.Target == TexTarget.RenderTexture &&
                     ((SupportedByRenderTexturePair && !id.RenderTexture)
                      || (SupportedBySingleBuffer && id.RenderTexture)));
            }

            public virtual void Inspect()
            {
                if (Brush.InspectedIsCpuBrush || !Painter.Camera)
                    return;

                var brush = InspectedBrush;

                var adv = InspectAdvanced;

               // var p = InspectedPainter;

                if (Brush.showAdvanced || brush.useMask)
                {

                    if (adv || brush.useMask)
                        "Mask".PL("Multiply Brush Speed By Mask Texture's alpha").ToggleIcon(ref brush.useMask, true);

                    if (brush.useMask)
                    {

                        pegi.SelectOrAdd(ref brush.selectedSourceMask, ref Painter.Data.masks).Nl();

                        if (adv)
                            "Mask greyscale".PL("Otherwise will use alpha").ToggleIcon(ref brush.maskFromGreyscale)
                                .Nl();

                        if (brush.flipMaskAlpha || adv)
                            "Flip Mask ".PL("Alpha = 1-Alpha").ToggleIcon(ref brush.flipMaskAlpha).Nl();

                        if (!brush.randomMaskOffset && adv)
                            "Mask Offset ".PL().Edit_01(ref brush.maskOffset).Nl();

                        if (brush.randomMaskOffset || adv)
                            "Random Mask Offset".PL().ToggleIcon(ref brush.randomMaskOffset).Nl();

                        if (adv)
                            if ("Mask Tiling: ".ConstL().Edit(ref brush.maskTiling, 1, 8).Nl())
                                brush.maskTiling = Mathf.Clamp(brush.maskTiling, 0.1f, 64);
                    }
                }

                pegi.Nl();

            }

            #endregion
            
            public virtual void OnShaderBrushUpdate(Brush brush)
            {

            }

            public virtual void PaintPixelsInRam(Painter.Command.Base command)
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

                if (id[TextureStateFlags.DisableContiniousLine])
                {

                    st.uvFrom = st.uvTo;
                    st.posFrom = st.posTo;

                }
                else
                {

                    var uvDist = (deltaUv.magnitude * id.Width * 8 / br.Size(false));
                    var worldDist = st.DeltaWorldPos.magnitude;

                    steps = (int) Mathf.Max(1, IsAWorldSpaceBrush ? worldDist : uvDist);

                    deltaUv /= steps;
                    deltaPos /= steps;

                    st.uvFrom += deltaUv;
                    st.posFrom += deltaPos;
                }

                Action<Painter.Command.Base> blitMethod = null;

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

            public virtual void PaintRenderTextureInWorldSpace(Painter.Command.WorldSpaceBase command) { }

            public virtual void PaintRenderTextureUvSpace(Painter.Command.Base command) 
            {
                TextureMeta textureMeta = command.TextureData;
                Brush br = command.Brush;
                Stroke st = command.Stroke;

                BeforeStroke(command); 

                if (st.CrossedASeam())
                    st.uvFrom = st.uvTo;

                command.strokeAlphaPortion = Mathf.Clamp01(br.Flow * 0.05f);

                Painter.Camera.SHADER_STROKE_SEGMENT_UPDATE(command); 

                var rb = RtBrush;

                rb.localScale = Vector3.one;
                var direction = st.DeltaUv;
                var length = direction.magnitude;
                BrushMesh = Painter.BrushMeshGenerator.GetLongMesh(length * 256, br.StrokeWidth(textureMeta.Width, false));
                
                rb.SetLocalPositionAndRotation(Stroke.GetCameraProjectionTarget((st.uvFrom + st.uvTo) * 0.5f), Quaternion.Euler(new Vector3(0, 0,
                    (direction.x > 0 ? -1 : 1) * Vector2.Angle(Vector2.up, direction))));

                Painter.Camera.Render();
                
                AfterStroke(command);

            }
            
            public void BeforeStroke(Painter.Command.Base command)
            {
                RenderTextureBuffersManager.UpdateSecondBuffer();

                Painter.Command.ForPainterComponent painterCommand = command as Painter.Command.ForPainterComponent;
                if (painterCommand!= null)
                    foreach (var p in painterCommand.painter.Modules)
                        p.BeforeGpuStroke(painterCommand);
            }

            public virtual void AfterStroke(Painter.Command.Base command)
            {
                Brush brush = command.Brush;
                Stroke stroke = command.Stroke;
                TextureMeta textureData = command.TextureData;

                command.OnStrokeComplete();

                if (brush.useMask && stroke.MouseUpEvent && brush.randomMaskOffset)
                    brush.maskOffset = new Vector2(Random.Range(0f, 1f), Random.Range(0f, 1f));

                Painter.Command.ForPainterComponent painterCommand = command as Painter.Command.ForPainterComponent;

                if (command.usedAlphaBuffer)
                {
                    var sh = brush.GetBlitMode(TexTarget.RenderTexture).ShaderForAlphaBufferBlit;
                    if (painterCommand==null || painterCommand.painter.NotUsingPreview)
                        Painter.Camera.UpdateFromAlphaBuffer(textureData.CurrentRenderTexture(), sh);
                    else
                        Painter.Camera.AlphaBufferSetDirtyBeforeRender(textureData, sh);
                }
                else if (!brush.IsSingleBufferBrush() && !command.Is3DBrush)
                    Painter.Camera.UpdateBufferSegment();

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

            public override void PaintRenderTextureUvSpace(Painter.Command.Base command)
            {
                Brush br = command.Brush;
                Stroke st = command.Stroke;
                TextureMeta id = command.TextureData;
                BeforeStroke(command);

                if (st.CrossedASeam())
                    st.uvFrom = st.uvTo;
                
                command.strokeAlphaPortion = Mathf.Clamp01(br.Flow * 0.05f);

                Painter.Camera.SHADER_STROKE_SEGMENT_UPDATE(command);// br, br.Speed * 0.05f, id, st, out alphaBuffer, painter);

                RtBrush.localScale = Vector3.one * br.StrokeWidth(id.Width, false);

                BrushMesh = Painter.BrushMeshGenerator.GetQuad();
                
                RtBrush.SetLocalPositionAndRotation(st.CameraProjectionTarget, Quaternion.identity);

                Painter.Camera.Render();

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

                var command = new Painter.Command.UV(new Stroke(uv)
                {
                    firstStroke = false
                }, rt.GetTextureMeta(), br)
                {
                    strokeAlphaPortion = Mathf.Clamp01(br.Flow * 0.05f)
                };

                Painter.Camera.SHADER_STROKE_SEGMENT_UPDATE(command); // br, br.Speed * 0.05f, id, stroke, out alphaBuffer);

                float width = br.StrokeWidth(command.TextureData.Width, false);

                RtBrush.localScale = Vector3.one;

                BrushMesh = Painter.BrushMeshGenerator.GetLongMesh(0, width);
                
                RtBrush.SetLocalPositionAndRotation(Stroke.GetCameraProjectionTarget(command.Stroke.uvTo), Quaternion.Euler(new Vector3(0, 0, Vector2.Angle(Vector2.up, Vector2.zero))));

                Painter.Camera.Render();

                br.GetBrushType(TexTarget.RenderTexture).AfterStroke(command); 

            }

            public static void Paint(RenderTexture renderTexture, Brush br, Stroke st) =>
                _inst.PaintRenderTextureUvSpace(new Painter.Command.UV(st, renderTexture, br));

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


            public override bool SupportsAlphaBufferPainting => false;

            protected override string ShaderKeyword(bool texcoord) => "BRUSH_DECAL";
            public override bool IsUsingDecals => true;

            private Vector2 _previousUv;


            private readonly ShaderProperty.TextureValue _decalHeightProperty =
                new("_VolDecalHeight");

            private readonly ShaderProperty.TextureValue _decalOverlayProperty =
                new("_VolDecalOverlay");

            private readonly ShaderProperty.VectorValue _decalParametersProperty =
                new("_DecalParameters");


            public override void OnShaderBrushUpdate(Brush brush)
            {
                var vd = Painter.Data.decals.TryGet(brush.selectedDecal);

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

            public override void PaintRenderTextureUvSpace( Painter.Command.Base command) //PlaytimePainter painter, Brush br, Stroke st)
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

                        var portion = Mathf.Clamp01(delta.magnitude * id.Width * 4 / br.Size(false));

                        var newAngle = Vector2.SignedAngle(Vector2.up, delta) + br.decalAngleModifier;
                        br.decalAngle = Mathf.LerpAngle(br.decalAngle, newAngle, portion);

                        _previousUv = st.uvTo;

                    }

                    command.strokeAlphaPortion = 1;

                    Painter.Camera.SHADER_STROKE_SEGMENT_UPDATE(command); // br, 1, id, st, out alphaBuffer, command.painter);
                    var tf = RtBrush;
                    tf.localScale = Vector3.one * br.Size(false);
                    tf.localRotation = Quaternion.Euler(new Vector3(0, 0, br.decalAngle));
                    BrushMesh = Painter.BrushMeshGenerator.GetQuad();

                    st.uvTo = st.uvTo.To01Space();

                    var deltaUv = st.DeltaUv;

                    var uv = st.uvTo;

                    if (br.rotationMethod == RotationMethod.FaceStrokeDirection && !st.firstStroke)
                    {
                        var length = Mathf.Max(deltaUv.magnitude * 2 * id.Width / br.Size(false), 1);
                        var scale = tf.localScale;

                        if ((Mathf.Abs(Mathf.Abs(br.decalAngleModifier) - 90)) < 40)
                            scale.x *= length;
                        else
                            scale.y *= length;

                        tf.localScale = scale;
                        uv -= deltaUv * ((length - 1) * 0.5f / length);
                    }

                    tf.localPosition = Stroke.GetCameraProjectionTarget(uv);

                    Painter.Camera.Render();

                    AfterStroke(command);

                }
                else
                    command.OnStrokeComplete(); //painter.AfterStroke(st);
            }

            public override void AfterStroke(Painter.Command.Base command)
            {
                Brush br = command.Brush;

                base.AfterStroke(command);

                if (br.rotationMethod != RotationMethod.Random) return;

                br.decalAngle = Random.Range(-90f, 450f);
                OnShaderBrushUpdate(Painter.Data.Brush);
            }

            #region Inspector

            protected override MsgPainter Translation => MsgPainter.BrushTypeDecal;
            
           public override void Inspect()
            {

                pegi.Select_Index(ref InspectedBrush.selectedDecal, Painter.Data.decals);

                var decal = Painter.Data.decals.TryGet(InspectedBrush.selectedDecal);

                if (decal == null)
                {
                    pegi.FullWindow.WarningDocumentationClickOpen("Select a valid decal. You can add some in Config -> Lists.","No Decal selected");
                }

                pegi.Nl();
                
                "Continuous".PL("Will keep adding decal every frame while the mouse is down", 80).Toggle(
                    ref InspectedBrush.decalContentious);

                pegi.FullWindow.DocumentationClickOpen("Continious Decal will keep painting every frame while mouse button is held", "Countinious Decal");

                pegi.Nl();

                "Rotation".PL("Rotation method", 60).Write();

                pegi.Edit_Enum(ref InspectedBrush.rotationMethod).Nl();

                switch (InspectedBrush.rotationMethod)
                {
                    case RotationMethod.Constant:
                        "Angle:".PL("Decal rotation", 60).Write();
                        pegi.Edit(ref InspectedBrush.decalAngle, -90, 450);
                        break;
                    case RotationMethod.FaceStrokeDirection:
                        "Ang Offset:".PL("Angle modifier after the rotation method is applied", 80).Edit(
                            ref InspectedBrush.decalAngleModifier, -180f, 180f);
                        break;
                }

                pegi.Nl();
                if (!InspectedBrush.mask.HasFlag(ColorMask.A))
                    "! Alpha chanel is disabled. Decals may not render properly".PL().Write_Hint();

            }

            #endregion
        }

        public enum VolumetricDecalType
        {
            Add,
            Dent
        }

        [Serializable]
        public class VolumetricDecal : IInspectorDropdown, IPEGI, IGotStringId
        {
            public string decalName;
            public VolumetricDecalType type;
            public Texture2D heightMap;
            public Texture2D overlay;

            #region Inspector
            
            public bool ShowInInspectorDropdown() => heightMap && overlay;

            public string StringId
            {
                get { return decalName; }
                set { decalName = value; }
            }

            void IPEGI.Inspect()
            {
                this.inspect_Name().Nl();

                "Brush Type".ConstL().Edit_Enum(ref type).Nl();
                "Height Map".PL().Edit(ref heightMap).Nl();
                "Overlay".PL().Edit(ref overlay).Nl();
            }

            public override string ToString() => "{0} ({1})".F(decalName, type);

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

            public override void PaintRenderTextureUvSpace(Painter.Command.Base command)//PlaytimePainter painter, Brush br, Stroke st)
            {

                Brush br = command.Brush;
                Stroke st = command.Stroke;
                var id = command.TextureData;

                BeforeStroke(command);

                var deltaUv = st.DeltaUv; //uv - st.uvFrom;//.Previous_uv;
                var magnitude = deltaUv.magnitude;
                
                var width = br.Size(false) / id.Width * 4;

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

                var r = Painter.Camera;

                var meshWidth = br.StrokeWidth(id.Width, false);

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
                    BrushMesh = Painter.BrushMeshGenerator.GetStreak(UvToPosition(st.uvFrom),
                        UvToPosition(junkPoint), meshWidth, true, false);
                    tf.localScale = Vector3.one;
                    tf.SetLocalPositionAndRotation(new Vector3(0, 0, 10), Quaternion.identity);


                    r.Render();
                    st.uvFrom = junkPoint;
                    isTail = true;
                }

                command.strokeAlphaPortion = Mathf.Clamp01(br.Flow * 0.05f);

                r.SHADER_STROKE_SEGMENT_UPDATE(command);//br, br.Speed * 0.05f, id, st, out alphaBuffer, painter);

                BrushMesh = Painter.BrushMeshGenerator.GetStreak(UvToPosition(st.uvFrom), UvToPosition(st.uvTo),
                    meshWidth, st.MouseUpEvent, isTail);
                tf.localScale = Vector3.one;
                tf.SetLocalPositionAndRotation(new Vector3(0, 0, 10), Quaternion.identity);

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

            public override bool NeedsGrid => Painter.Data.useGridForBrush;

            private static void PrepareSphereBrush(Painter.Command.WorldSpaceBase command)
            {
                Brush br = command.Brush;
                var td = command.TextureData;

                command.strokeAlphaPortion = Mathf.Clamp01(br.Flow * 0.05f);

                Painter.Camera.SHADER_STROKE_SEGMENT_UPDATE(command); 

                var offset = command.TextureData.Offset - command.Stroke.unRepeatedUv.Floor();

                command.Stroke.FeedWorldPosInShader();

                PainterShaderVariables.BRUSH_EDITED_UV_OFFSET.GlobalValue = new Vector4(td.Tiling.x, td.Tiling.y, offset.x, offset.y);
                PainterShaderVariables.BRUSH_ATLAS_SECTION_AND_ROWS.GlobalValue = new Vector4(0, 0, 1, 0);
            }

            public override void PaintRenderTextureInWorldSpace(Painter.Command.WorldSpaceBase command)
            {
               
                BeforeStroke(command);
                PrepareSphereBrush(command); 

               // if (!command.Stroke.MouseDownEvent)
               // {
                 Painter.Camera.Prepare(command).Render();
               // }

                AfterStroke(command);
            }

            public static void Paint(Painter.Command.WorldSpaceBase command)
            {
                Brush br = command.Brush;
                br.GetBlitMode(command.TextureData.Target).PrePaint(command);
                PrepareSphereBrush(command); 
                Painter.Camera.Prepare(command).Render();
                br.GetBrushType(command.TextureData.Target).AfterStroke(command);
            }


            public override void PaintRenderTextureUvSpace(Painter.Command.Base command) 
            {
                Debug.LogError("{0} does not implemet {1}".F(nameof(Sphere), nameof(PaintRenderTextureUvSpace)));
            }


            public static void PaintAtlased(Painter.Command.WorldSpaceBase command,int aTexturesInRow)
            {
                PainterShaderVariables.BRUSH_ATLAS_SECTION_AND_ROWS.GlobalValue = new Vector4(0, 0, aTexturesInRow, 1);

                Paint(command);

                PainterShaderVariables.BRUSH_ATLAS_SECTION_AND_ROWS.GlobalValue = new Vector4(0, 0, 1, 0);
            }

            #region Inspector

            protected override MsgPainter Translation => MsgPainter.BrushTypeSphere;

           public override void Inspect()
            {

                var br = InspectedBrush;

                bool suggestGrid = false;

                if (InspectedPainter && !InspectedPainter.GetMesh())
                {
                    if (!Painter.Data.useGridForBrush)
                    {
                        "No mesh for sphere painting detected.".PL().WriteWarning();
                        suggestGrid = true;
                    }
                }

                if (InspectAdvanced || Painter.Data.useGridForBrush || suggestGrid)
                {
                    (Painter.Data.useGridForBrush
                        ? ("Grid: Z, X - change plane  |  Ctrl+LMB - reposition GRID")
                        : "Paint On Grid").PL().ToggleIcon(ref Painter.Data.useGridForBrush).Nl();

                    pegi.Line();
                    pegi.Nl();
                }

                if (!br.useAlphaBuffer && (br.worldSpaceBrushPixelJitter || InspectAdvanced))
                {
                    "One Pixel Jitter".PL().ToggleIcon(ref br.worldSpaceBrushPixelJitter);
                    pegi.FullWindow.DocumentationClickOpen("Will provide a single pixel jitter which can help fix seams not being painted properly", "Why use one pixel jitter?");
                    pegi.Nl();
                }

                base.Inspect();

                pegi.Nl();
            }

            #endregion
        }
    }
}