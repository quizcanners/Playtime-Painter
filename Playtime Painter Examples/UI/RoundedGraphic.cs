using System;
using System.Collections;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PlaytimePainter.Examples
{

    [ExecuteInEditMode]
    public class RoundedGraphic : Image, IKeepMyCfg, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPEGI {

        #region Shader MGMT

        public bool feedPositionData = true;

        [SerializeField] private Rect atlasedUVs = new Rect(0,0,1,1);

        private enum PositionDataType { ScreenPosition, AtlasPosition }

        [SerializeField] private PositionDataType _positionDataType;

        [SerializeField] private float[] _roundedCorners = new float[1];
        
        private float GetCorner(int index) => _roundedCorners[index % _roundedCorners.Length];

        private void SetCorner(int index, float value) => _roundedCorners[index % _roundedCorners.Length] = value;

        private void SetCorners(float value)
        {
            for (int i=0; i< _roundedCorners.Length; i++)
                _roundedCorners[i] = value;
        }

        public bool LinkedCorners
        {
            get { return _roundedCorners.Length == 1; }

            set
            {
                var targetValue = value ? 1 : 4;

                if (targetValue == _roundedCorners.Length) return;

                if (material)
                    material.SetShaderKeyword(UNLINKED_VERTICES, targetValue>1);

                var tmp = _roundedCorners[0];

                _roundedCorners = new float[targetValue];

                for (var i = 0; i < _roundedCorners.Length; i++)
                    _roundedCorners[i] = tmp;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {

            if (!gameObject.activeSelf) {
                if (Debug.isDebugBuild && Application.isEditor)
                    Debug.LogError("On populate mesh is called for disabled UI element");

                return;
            }

            var rt = rectTransform;
            var piv = rt.pivot;
            var rectSize = rt.rect.size;
           
            var corner1 = (Vector2.zero - piv) * rectSize;
            var corner2 = (Vector2.one - piv) * rectSize;

            vh.Clear();

            var vertex = UIVertex.simpleVert;

            var pos = Vector2.zero;

            var myCanvas = canvas;

            if (feedPositionData) {
               
                pos = RectTransformUtility.WorldToScreenPoint(IsOverlay ? null : (myCanvas ? myCanvas.worldCamera : null), rt.position);
               
            }
            var rctS = rectSize;

            float rectDiff = rctS.x - rctS.y;

            rctS = new Vector2(Mathf.Max(0, rectDiff / rctS.x), Mathf.Max(0, (-rectDiff) / rctS.y));

            var scaleToSided = rctS.x - rctS.y; // If x>0 - positive, else - negative

            var feedAtlasedPosition = feedPositionData && _positionDataType == PositionDataType.AtlasPosition;

            float atlasedAspectX = 0;
            
            var sp = sprite;
            if (sp) {
                var tex = sp.texture;

                var texturePixelSize = new Vector2(tex.width, tex.height);

                var pixelsToShow = texturePixelSize;
 
                if (feedAtlasedPosition)
                    pixelsToShow *= new Vector2( (atlasedUVs.width - atlasedUVs.x), (atlasedUVs.height - atlasedUVs.y));
                
                var sizeByPixels = rectSize / pixelsToShow;

                var scaleX = Mathf.Max(0, (sizeByPixels.y - sizeByPixels.x) / sizeByPixels.y);

                var truePixSizeX = (rectSize.x * (myCanvas ? myCanvas.scaleFactor : 1)); 
                
                atlasedAspectX = (pixelsToShow.x * (1f - scaleX)) / truePixSizeX;
                
                if (feedAtlasedPosition) {
                    var atlasedOffCenter = (new Vector2(atlasedUVs.width + atlasedUVs.x, atlasedUVs.height + atlasedUVs.y)  - Vector2.one)*0.5f;

                    pos -= atlasedOffCenter * texturePixelSize / atlasedAspectX;
                }
            }

            if (feedPositionData)
                pos.Scale(new Vector2(1f / Screen.width, 1f / Screen.height));
            
            vertex.normal = new Vector3(pos.x, pos.y, atlasedAspectX);

            vertex.uv1 = new Vector2(scaleToSided, GetCorner(0));  
            vertex.color = color;
            
            vertex.uv0 = new Vector2(0, 0);
            vertex.position = new Vector2(corner1.x, corner1.y);
            vh.AddVert(vertex);

            vertex.uv0 = new Vector2(0, 1);
            vertex.uv1.y = GetCorner(1);
            vertex.position = new Vector2(corner1.x, corner2.y);
            vh.AddVert(vertex);
            
            vertex.uv0 = new Vector2(1, 1);
            vertex.uv1.y = GetCorner(2);
            vertex.position = new Vector2(corner2.x, corner2.y);
            vh.AddVert(vertex);
            
            vertex.uv0 = new Vector2(1, 0);
            vertex.uv1.y = GetCorner(3);
            vertex.position = new Vector2(corner2.x, corner1.y);
            vh.AddVert(vertex);

            if (LinkedCorners)
            {
                //1  2
                //0  3
                vh.AddTriangle(0, 1, 2);
                vh.AddTriangle(2, 3, 0);
            }
            else
            {
                //1    6,9    2
                //7  13  14   8
                //4  12  15   11
                //0    5,10   3

                var cornMid = (corner1 + corner2) * 0.5f;
                
                vertex.uv1.y = GetCorner(0);
                vh.AddVert(vertex.Set(0, 0.5f, corner1, cornMid)); //4
                vh.AddVert(vertex.Set(0.5f, 0, cornMid, corner1)); //5

                vertex.uv1.y = GetCorner(1);
                vh.AddVert(vertex.Set(0.5f, 1, cornMid, corner2)); //6
                vh.AddVert(vertex.Set(0, 0.5f, corner1, cornMid)); //7

                vertex.uv1.y = GetCorner(2);
                vh.AddVert(vertex.Set(1, 0.5f, corner2, cornMid)); //8
                vh.AddVert(vertex.Set(0.5f, 1, cornMid, corner2)); //9

                vertex.uv1.y = GetCorner(3);
                vh.AddVert(vertex.Set(0.5f, 0, cornMid, corner1)); //10
                vh.AddVert(vertex.Set(1, 0.5f, corner2, cornMid)); //11

                vertex.uv1.y = GetCorner(0);
                vh.AddVert(vertex.Set(0.5f, 0.5f, cornMid, cornMid)); //12

                vertex.uv1.y = GetCorner(1);
                vh.AddVert(vertex.Set(0.5f, 0.5f, cornMid, cornMid)); //13

                vertex.uv1.y = GetCorner(2);
                vh.AddVert(vertex.Set(0.5f, 0.5f, cornMid, cornMid)); //14

                vertex.uv1.y = GetCorner(3);
                vh.AddVert(vertex.Set(0.5f, 0.5f, cornMid, cornMid)); //15

                vh.AddTriangle(0, 4, 5);
                vh.AddTriangle(1, 6, 7);
                vh.AddTriangle(2, 8, 9);
                vh.AddTriangle(3, 10, 11);

                vh.AddTriangle(12, 5, 4);
                vh.AddTriangle(13, 7, 6);
                vh.AddTriangle(14, 9, 8);
                vh.AddTriangle(15, 11, 10);

            }
        }
        
        private bool IsOverlay
        {
            get
            {
                var c = canvas;
                return c && (c.renderMode == RenderMode.ScreenSpaceOverlay || !c.worldCamera);
            }
        }
        
        public const string UNLINKED_VERTICES = "_UNLINKED";
        public const string EDGE_SOFTNESS_FLOAT = "_Edges";
        
        #endregion

        #region Inspector
        #if PEGI

        private static List<Shader> _compatibleShaders;

        public static List<Shader> CompatibleShaders =>
            _compatibleShaders ?? (_compatibleShaders = new List<Shader>()
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/BumpedButton"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Box"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/PixelPerfect"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Outline"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/ButtonWithShadow"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Shadow"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Gradient"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/PreserveAspect"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/PreserveAspect_InvertingFiller"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/SubtractiveGraphic"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Primitives/PixelLine")));

        [SerializeField] private bool _showModules;
        [SerializeField] private int _inspectedModule;
        public static RoundedGraphic inspected;

        public bool Inspect()
        {
            inspected = this;

            pegi.toggleDefaultInspector();
            
            Msg.RoundedGraphic.DocumentationClick();

            pegi.nl();
            
            var mat = material;

            var can = canvas;
            
            var shad = mat.shader;
            
            var changed = false;

            var expectedScreenPosition = false;

            var expectedAtlasedPosition = false;

            if (!_showModules)
            {

                bool gotPixPerfTag = false;

                #region Material Tags 
                if (mat)
                {
                    var pixPfTag = mat.Get(ShaderTags.PixelPerfectUi);

                    gotPixPerfTag = !pixPfTag.IsNullOrEmpty();

                    if (!gotPixPerfTag)
                        "{0} doesn't have {1} tag".F(shad.name, ShaderTags.PixelPerfectUi.NameForDisplayPEGI).writeWarning();
                    else
                    {

                        expectedScreenPosition = pixPfTag.Equals(ShaderTags.PixelPerfectUis.Position.NameForDisplayPEGI);

                        if (!expectedScreenPosition)
                        expectedAtlasedPosition = pixPfTag.Equals(ShaderTags.PixelPerfectUis.AtlasedPosition.NameForDisplayPEGI);

                        if (!can)
                            "No Canvas".writeWarning();
                        else
                        {
                            if ((can.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord1) == 0)
                            {

                                "Material requires Canvas to pass Edges data trough Texture Coordinate 1 data channel"
                                    .writeWarning();
                                if ("Fix Canvas Texture Coordinate 1".Click().nl())
                                    can.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
                            }

                            if (feedPositionData &&
                                ((can.additionalShaderChannels & AdditionalCanvasShaderChannels.Normal) == 0))
                            {
                                "Material requires Canvas to pass Position Data trough Normal channel".writeWarning();
                                if ("Fix Canvas ".Click().nl())
                                    can.additionalShaderChannels |= AdditionalCanvasShaderChannels.Normal;
                            }

                            if (can.renderMode == RenderMode.WorldSpace) {
                                "Rounded UI isn't working on world space UI yet.".writeWarning();
                                if ("Change to Overlay".Click())
                                    can.renderMode = RenderMode.ScreenSpaceOverlay;
                                if ("Change to Camera".Click())
                                    can.renderMode = RenderMode.ScreenSpaceCamera;
                                pegi.nl();
                            }

                        }
                    }
                }
                #endregion

                var linked = LinkedCorners;

              
                if (mat && (linked == mat.IsKeywordEnabled(UNLINKED_VERTICES)))
                    mat.SetShaderKeyword(UNLINKED_VERTICES, !linked);

                if (pegi.toggle(ref linked, icon.Link, icon.UnLinked).changes(ref changed))
                    LinkedCorners = linked;

                for (var i = 0; i < _roundedCorners.Length; i++) {
                    var crn = _roundedCorners[i];

                    if ("Corner{0}".F(linked ? "s" : (" " + i)).edit(70, ref crn, 0, 1f).nl(ref changed))
                        _roundedCorners[i] = crn;
                }

                pegi.nl();
                
                if (mat) {

                    if (!Application.isPlaying) {

                        var path = mat.GetAssetFolder();
                        if (path.IsNullOrEmpty())
                            "Material is not saved as asset. Click COPY next to it to save as asset".writeHint();
                    }

                    var n = mat.name;
                    if ("Rename Material".editDelayed("Press Enter to finish renaming." ,120, ref n))
                        mat.RenameAsset(n);
                }

                if (pegi.edit(ref mat, 60).changes(ref changed) || pegi.ClickDuplicate(ref mat, gameObject.name).nl(ref changed))
                    material = mat;

                if (mat) {

                    if ("Shaders".select(60, ref shad, CompatibleShaders, false, true).changes(ref changed))
                        mat.shader = shad;
                    
                    var sTip = mat.Get(QuizCannersUtilities.ShaderTags.ShaderTip);

                    if (!sTip.IsNullOrEmpty())
                        sTip.fullWindowDocumentationClickOpen("Tip from shader tag");

                    if (icon.Refresh.Click("Refresh compatible Shaders list"))
                        _compatibleShaders = null;
                }

                pegi.nl();

                #region Position Data

                if (expectedScreenPosition || expectedAtlasedPosition || feedPositionData) {
                    "Position Data".toggleIcon(ref feedPositionData, true).changes(ref changed);
                    if (feedPositionData) {
                        "Position: ".editEnum(60, ref _positionDataType).changes(ref changed);

                        "Shaders that use position data often don't look right in the scene view."
                            .fullWindowDocumentationClickOpen("Camera dependancy warning");

                        pegi.nl();

                        if (gotPixPerfTag)
                        {

                            if (_positionDataType == PositionDataType.AtlasPosition) {

                                if (expectedScreenPosition)
                                    "Shader is expecting Screen Position".writeWarning();
                            }
                            else if (expectedAtlasedPosition)
                                "Shader is expecting Atlased Position".writeWarning();
                        }
                    }

                    pegi.nl();
                }

                if (gotPixPerfTag && feedPositionData)  {
                    if (!expectedScreenPosition && !expectedAtlasedPosition)
                        "Shader doesn't have any PixelPerfectUI Position Tags. Position updates may not be needed".writeWarning();
                 
                        pegi.nl();

                    if (rectTransform.pivot != Vector2.one * 0.5f) {
                        "Pivot is expected to be in the center for position processing to work".writeWarning();
                        pegi.nl();
                        if ("Set Pivot to 0.5,0.5".Click().nl(ref changed))
                            rectTransform.SetPivotTryKeepPosition(Vector2.one * 0.5f);
                    }

                    if (rectTransform.localScale != Vector3.one) {
                        "Scale deformation can interfear with some shaders that use position".writeWarning();
                        pegi.nl();
                        if ("Set local scale to 1".Click().nl(ref changed))
                            rectTransform.localScale = Vector3.one;
                    }

                    if (rectTransform.localRotation != Quaternion.identity)
                    {
                        "Rotation can compromise calculations in shaders that need position".writeWarning();
                        if ("Reset Rotation".Click().nl(ref changed))
                            rectTransform.localRotation = Quaternion.identity;
                        
                    }

                    if (_positionDataType == PositionDataType.AtlasPosition) {
                            "UV:".edit(ref atlasedUVs).nl(ref changed);
                            pegi.edit01(ref atlasedUVs).nl(ref changed);
                    }

                }

                #endregion

                var rt = raycastTarget;
                if ("Click-able".toggleIcon("Is RayCast Target", ref rt).nl(ref changed))
                    raycastTarget = rt;
                
                if (rt)
                    "On Click".edit_Property(() => OnClick, this).nl(ref changed);

                var spriteTag = mat ? mat.Get(ShaderTags.SpriteRole) : null;

                var noTag = spriteTag.IsNullOrEmpty();

                if (noTag || !spriteTag.SameAs(ShaderTags.SpriteRoles.Hide.NameForDisplayPEGI)) {

                    if (noTag)
                        spriteTag = "Sprite";

                    var sp = sprite;
                    if (spriteTag.edit(90, ref sp).changes(ref changed))
                        sprite = sp;
                    
                    if (sp) {

                        var tex = sp.texture;

                        if (tex && (tex.width != rectTransform.rect.width || sp.texture.height != rectTransform.rect.height) && icon.Size.Click("Set Native Size").nl()) 
                            rectTransform.sizeDelta = new Vector2(tex.width, tex.height);
                        
                    }

                    pegi.nl();

                }

                var col = color;
                if (pegi.edit(ref col).nl(ref changed))
                    color = col;
            }
            
            if ("Modules".enter_List(ref _modules, ref _inspectedModule, ref _showModules).nl(ref changed))
                this.SaveStdData();
            
            if (changed)
                SetVerticesDirty();
            
            return changed;
        }
        #endif
        #endregion

        #region Modules
        
        private List<RoundedButtonModuleBase> _modules = new List<RoundedButtonModuleBase>();
        
        [SerializeField] private string _modulesStd = "";

        public string ConfigStd
        {
            get { return _modulesStd;  }
            set { _modulesStd = value; }
        }

        protected override void OnEnable() {
            base.OnEnable();
            this.LoadStdData();
        }

        protected override void OnDisable() {
            base.OnDisable();
            if (!Application.isPlaying)
                this.SaveStdData();
        }
        
        public CfgEncoder Encode() => new CfgEncoder()
            .Add_Abstract("mdls", _modules);

        public void Decode(string data) => data.DecodeTagsFor(this);

        public bool Decode(string tg, string data) {

            switch (tg) {
                case "mdls": data.Decode_List(out _modules, RoundedButtonModuleBase.all); break;
                default: return false;
            }

            return true;
        }

        #endregion

        #region Mouse Press

        public UnityEvent OnClick;

        public float maxHoldForClick = 0.3f;
        public float maxMousePositionPixOffsetForClick = 20f;
        
        [NonSerialized] private bool _mouseDown;
        [NonSerialized] private float _mouseDownTime;
        [NonSerialized] private Vector2 _mouseDownPosition;
        [NonSerialized] private bool _mouseOver;

        public void OnPointerEnter(PointerEventData eventData) => _mouseOver = true;

        public void OnPointerExit(PointerEventData eventData)
        {
            _mouseDown = false;
            _mouseOver = false;
        } 

        public void OnPointerDown(PointerEventData eventData) {
            _mouseDownPosition = Input.mousePosition;
            _mouseDown = true;
            _mouseDownTime = Time.time;
        }

        public void OnPointerUp(PointerEventData eventData) {

            if (_mouseDown && Time.time - _mouseDownTime < maxHoldForClick) {

                var diff = _mouseDownPosition - Input.mousePosition.ToVector2();

                if ((diff.magnitude) < maxMousePositionPixOffsetForClick)
                    OnClick.Invoke();
            }

            _mouseDown = false;
        }

        #endregion
        
        private Vector3 _previousPos = Vector3.zero;
        
        private void Update() {

            var needsUpdate = false;

            foreach (var m in _modules)
                needsUpdate |= m.Update(this);

            if (feedPositionData && rectTransform.position != _previousPos)  {
                needsUpdate = true;
                _previousPos = rectTransform.position;
            }

            if (needsUpdate)
                SetAllDirty();
        }

        #region Rounded Button Modules

        [TaggedType(Tag, "Native Size from Tiled Texture")]
        public class RoundedButtonNativeSizeForOverlayOffset : RoundedButtonModuleBase, IPEGI, IPEGI_ListInspect
        {

            private const string Tag = "TiledNatSize";

            private ShaderProperty.TextureValue referenceTexture = new ShaderProperty.TextureValue("_MainTex");

            public override string ClassTag => Tag;
            
            #region Inspect
            #if PEGI
            public bool InspectInList(IList list, int ind, ref int edited)
            {

                var mat = inspected.material;
                if (mat)
                {

                    pegi.select_or_edit_TextureProperty(ref referenceTexture, mat);

                    var tex = referenceTexture.Get(mat);

                    if (tex) {
                        if (icon.Size.Click("Set Native Size for Texture, using it's Tile/Offset")) {

                            var size = new Vector2(tex.width, tex.height);
                            //var off = referenceTexture.GetOffset(mat);
                            var til = referenceTexture.GetTiling(mat);
                            size *= til;

                            inspected.rectTransform.sizeDelta = size;

                        }
                    }
                } else "No Material".write();

                return false;
            }

            public override bool Inspect()
            {
                var changed = base.Inspect();
                

                return changed;
            }
            #endif
            #endregion

            #region Encode & Decode

            public override bool Decode(string tg, string data) {

                switch (tg) {

                    case "b": data.Decode_Base(base.Decode, this); break;
                    case "mp": referenceTexture.Decode(data); break;
                    default: return false;
                }

                return true;
            }

            public override CfgEncoder Encode() => this.EncodeUnrecognized()
                    .Add("b", base.Encode())
                    .Add("mp", referenceTexture);

            #endregion
        }

        [TaggedType(Tag, "Change Corners on Click")]
        public class RoundedButtonCornersOnClick : RoundedButtonModuleBase, IPEGI, IPEGI_ListInspect {

            private const string Tag = "corners";

            public override string ClassTag => Tag;

            public float valueWhenOver = 0.5f;

            public float valueWhenOff = 0.5f;

            private readonly LinkedLerp.FloatValue _roundedCorners = new LinkedLerp.FloatValue();

            public override bool Update(RoundedGraphic target){

                _roundedCorners.targetValue = target._mouseOver ? valueWhenOver : valueWhenOff;

                var ld = new LerpData();

                _roundedCorners.Portion(ld);
                _roundedCorners.Lerp(ld);

                if (_roundedCorners.CurrentValue != target.GetCorner(0)) {
                    target.SetCorners(_roundedCorners.CurrentValue);
                    return true;
                }

                return false;
            }

            #region Inspect
            #if PEGI

            public bool InspectInList(IList list, int ind, ref int edited) {
                
                "Normal".edit(50, ref valueWhenOff, 0, 1);

                "On Hover".edit(50, ref valueWhenOver, 0, 1).nl();
                
                if (icon.Enter.Click())
                    edited = ind;

                return false;
            }

            public override bool Inspect()
            {
                var changed = base.Inspect();

                _roundedCorners.Nested_Inspect().nl(ref changed);

                return changed;
            }
            #endif
            #endregion

            #region Encode & Decode

            public override bool Decode(string tg, string data)
            {

                switch (tg)
                {
                    case "b": data.Decode_Base(base.Decode, this); break;
                    case "crn": _roundedCorners.Decode(data); break;
                    case "hov": valueWhenOver = data.ToFloat();  break;
                    case "nrm": valueWhenOff = data.ToFloat(); break;
                    default: return false;
                }

                return true;
            }

            public override CfgEncoder Encode() => this.EncodeUnrecognized()
                    .Add("b", base.Encode())
                    .Add("crn", _roundedCorners)
                    .Add("hov", valueWhenOver)
                    .Add("nrm", valueWhenOff);
            #endregion
        }
        
        public class RoundedButtonModuleAttribute : AbstractWithTaggedTypes
        {
            public override TaggedTypesCfg TaggedTypes => RoundedButtonModuleBase.all;

            public RoundedButtonModuleAttribute() { }

            public RoundedButtonModuleAttribute(params System.Type[] types) : base(types) { }

        }

        [RoundedButtonModule]
        public abstract class RoundedButtonModuleBase : AbstractKeepUnrecognizedCfg, IGotClassTag, IGotDisplayName
        {
            public static TaggedTypesCfg all = new TaggedTypesCfg(typeof(RoundedButtonModuleBase));
            public TaggedTypesCfg AllTypes => all;
            public abstract string ClassTag { get; }
            
            public virtual bool Update(RoundedGraphic target) => false;

            #region Inspect
            #if PEGI

            public virtual string NameForDisplayPEGI => ClassTag;
            
            public override bool Inspect()
            {
                return false;
            }
            #endif
            #endregion

            #region Encode & Decode
            public override CfgEncoder Encode() => this.EncodeUnrecognized();

            public override bool Decode(string tg, string data) => false;
            #endregion
        }
        
        #endregion
    }


    public static partial class ShaderTags
    {
        public static readonly ShaderTag PixelPerfectUi = new ShaderTag("PixelPerfectUI");

        public static class PixelPerfectUis
        {
            public static readonly ShaderTagValue Simple = new ShaderTagValue("Simple", PixelPerfectUi);
            public static readonly ShaderTagValue Position = new ShaderTagValue("Position", PixelPerfectUi);
            public static readonly ShaderTagValue AtlasedPosition = new ShaderTagValue("AtlasedPosition", PixelPerfectUi);

        }


        public static readonly ShaderTag SpriteRole = new ShaderTag("SpriteRole");

        public static class SpriteRoles
        {
            public static readonly ShaderTagValue Hide = new ShaderTagValue("Hide", SpriteRole);
        }
    }

    public static class RoundedUiExtensions  {
        #if UNITY_EDITOR
        [MenuItem("GameObject/UI/Playtime Painter/Rounded UI Graphic", false, 0)]
        private static void CreateRoundedUiElement()
        {


            bool createdForSelection = false;

            if (Selection.gameObjects.Length > 0) {

                foreach (var go in Selection.gameObjects) {
                    if (go.GetComponentInParent<Canvas>()) {
                        CreateRoundedUiElement(go);
                        createdForSelection = true;
                    }
                }

            }
            
            if (!createdForSelection) {

                var canvas = Object.FindObjectOfType<Canvas>();

                if (!canvas)
                    canvas = new GameObject("Canvas").AddComponent<Canvas>();

                CreateRoundedUiElement(canvas.gameObject);

            }

        }

        private static void CreateRoundedUiElement(GameObject canvas) {
            var rg = new GameObject("Rounded UI Element").AddComponent<RoundedGraphic>();
            var go = rg.gameObject;
            GameObjectUtility.SetParentAndAlign(go, canvas);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }

        #endif

        public static UIVertex Set(this UIVertex vertex, float uvX, float uvY, Vector2 posX, Vector2 posY) {
            vertex.uv0 = new Vector2(uvX, uvY);
            vertex.position = new Vector2(posX.x, posY.y);
            return vertex;
        }
    }

    #region Inspector override
    #if UNITY_EDITOR
    [CustomEditor(typeof(RoundedGraphic))]
    public class PixelPerfectShaderDrawer : PEGI_Inspector_Mono<RoundedGraphic> { }
#endif

    public class PixelPerfectMaterialDrawer : PEGI_Inspector_Material
    {

        private static readonly ShaderProperty.FloatValue Softness = new ShaderProperty.FloatValue(RoundedGraphic.EDGE_SOFTNESS_FLOAT);

        private static readonly ShaderProperty.TextureValue Outline = new ShaderProperty.TextureValue("_OutlineGradient");

#if PEGI
        public override bool Inspect(Material mat)
        {

            var changed = pegi.toggleDefaultInspector();

            mat.edit(Softness, "Softness", 0, 1).nl(ref changed);

            mat.edit(Outline).nl(ref changed);

            if (mat.IsKeywordEnabled(RoundedGraphic.UNLINKED_VERTICES))
                "UNLINKED VERTICES".nl();

            var go = UnityUtils.GetFocusedGameObject();

            if (go)
            {

                var rndd = go.GetComponent<RoundedGraphic>();

                if (!rndd)
                    "No RoundedGrahic.cs detected, shader needs custom data.".writeWarning();
                else if (!rndd.enabled)
                    "Controller is disabled".writeWarning();

            }

            return changed;
        }
#endif

    }


    #endregion
}