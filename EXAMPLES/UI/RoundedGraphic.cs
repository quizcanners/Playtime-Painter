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

namespace Playtime_Painter.Examples
{

    [ExecuteInEditMode]
    public class RoundedGraphic : Image, IKeepMyCfg, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPEGI {

        #region Shader MGMT

        public bool feedPositionData = true;
        
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

            var rt = rectTransform;
            var piv = rt.pivot;
            var rectSize = rt.rect.size;
            var corner1 = (Vector2.zero - piv) * rectSize;
            var corner2 = (Vector2.one - piv) * rectSize;

            vh.Clear();

            var vertex = UIVertex.simpleVert;

            var pos = Vector2.zero;

            if (feedPositionData)
            {
                var myCanvas = canvas;
                pos = RectTransformUtility.WorldToScreenPoint(IsOverlay ? null : (myCanvas ? myCanvas.worldCamera : null), rt.position);
                pos.Scale(new Vector2(1f / Screen.width, 1f / Screen.height));
            }

            rectSize = new Vector2(Mathf.Max(0, (rectSize.x - rectSize.y) / rectSize.x), Mathf.Max(0, (rectSize.y - rectSize.x) / rectSize.y));

            var scaleToSided = rectSize.x - rectSize.y; // If x>0 - positive, else - negative

            vertex.normal = new Vector4(pos.x, pos.y, scaleToSided, 0);
            vertex.uv1 = new Vector2(scaleToSided, GetCorner(0));  // Replaced Edge smoothness with Scale
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

        public static List<Shader> CompatibleShaders
        {
            get
            {
                if (_compatibleShaders == null)
                {
                    _compatibleShaders = new List<Shader>()
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/BumpedButton"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Box"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/PixelPerfect"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Outline"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/ButtonWithShadow"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Shadow"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Gradient"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/PixelLine"));
                }
                return _compatibleShaders;
            }
        }

        [SerializeField] private bool _showModules;
        [SerializeField] private int _inspectedModule;
        public bool Inspect()
        {

            pegi.toggleDefaultInspector();

            var mat = material;

            var can = canvas;
            
            var shad = mat.shader;
            
            var changed = false;

            var usesPosition = false;

            if (!_showModules)
            {

                if (mat)
                {
                    var pixPfTag = mat.Get(ShaderTags.PixelPerfectUi);

                    if (pixPfTag.IsNullOrEmpty())
                        "{0} doesn't have {1} tag".F(shad.name, ShaderTags.PixelPerfectUi.NameForDisplayPEGI).writeWarning();
                    else
                    {

                        usesPosition = pixPfTag.SameAs("Position");

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
                        }
                    }
                }

                var linked = LinkedCorners;

                "Material Is Unlinked: {0}".F(mat.IsKeywordEnabled(UNLINKED_VERTICES)).nl();

                if (mat && (linked == mat.IsKeywordEnabled(UNLINKED_VERTICES)))
                    mat.SetShaderKeyword(UNLINKED_VERTICES, !linked);

                if (pegi.toggle(ref linked, icon.Link, icon.UnLinked).changes(ref changed))
                    LinkedCorners = linked;

                for (var i = 0; i < _roundedCorners.Length; i++)
                {
                    var crn = _roundedCorners[i];

                    if ("Corner{0}".F(linked ? "s" : (" " + i.ToString())).edit(90, ref crn, 0, 1f).nl(ref changed))
                        _roundedCorners[i] = crn;
                }

                pegi.nl();
                
                if (mat)
                {

                    if (!Application.isPlaying)
                    {
                        var path = mat.GetAssetFolder();
                        if (path.IsNullOrEmpty())
                            "Material is not saved as asset. Click COPY next to it to save as asset".writeHint();
                    }

                    var n = mat.name;
                    if ("Material".editDelayed(80, ref n))
                        mat.RenameAsset(n);
                }

                if (pegi.edit(ref mat, 60).changes(ref changed) || pegi.ClickDuplicate(ref mat, gameObject.name).nl(ref changed))
                    material = mat;

                if (mat)
                {

                    if ("Shaders".select(60, ref shad, CompatibleShaders, false, true).changes(ref changed))
                        mat.shader = shad;

                    if (icon.Refresh.Click("Refresh compatible Shaders list"))
                        _compatibleShaders = null;
                }

                pegi.nl();

                if (usesPosition || feedPositionData)
                    "Position Data".toggleIcon(ref feedPositionData).changes(ref changed);

                if (!usesPosition && feedPositionData)
                {
                    "Shader doesn't have PixelPerfectUI = Position Tag. Position updates may not be needed".write();
                    icon.Warning.write("Unnecessary data");
                }

                pegi.nl();

                var rt = raycastTarget;
                if ("Click-able".toggleIcon("Is RayCast Target", ref rt).nl(ref changed))
                    raycastTarget = rt;

                if (rt)
                    "On Click".edit_Property(() => OnClick, this).nl(ref changed);

                var spriteTag = mat ? mat.Get(ShaderTags.SpriteRole) : null;

                var noTag = spriteTag.IsNullOrEmpty();

                if (noTag || !spriteTag.SameAs("Hide"))
                {

                    if (noTag)
                        spriteTag = "Sprite";

                    var sp = sprite;
                    if (spriteTag.edit(90, ref sp).nl(ref changed))
                        sprite = sp;
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

        [TaggedType(Tag)]
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

                if (_roundedCorners.Value != target.GetCorner(0)) {
                    target.SetCorners(_roundedCorners.Value);
                    return true;
                }

                return false;
            }

            #region Inspect
#if PEGI
            public bool PEGI_inList(IList list, int ind, ref int edited) {
                
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
        }

        [RoundedButtonModule]
        public abstract class RoundedButtonModuleBase : AbstractKeepUnrecognizedCfg, IGotClassTag
        {
            public static TaggedTypesCfg all = new TaggedTypesCfg(typeof(RoundedButtonModuleBase));
            public TaggedTypesCfg AllTypes => all;
            public abstract string ClassTag { get; }

            public virtual bool Update(RoundedGraphic target) => false;

#region Inspect
#if PEGI
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
        public static readonly ShaderTag SpriteRole = new ShaderTag("SpriteRole");
    }

    public static class RoundedUiExtensions  {
#if UNITY_EDITOR
        [MenuItem("GameObject/UI/Playtime Painter/Rounded UI Graphic", false, 0)]
        private static void CreateRoundedUiElement() {

            var rg = new GameObject("Rounded UI Element").AddComponent<RoundedGraphic>();

            var go = rg.gameObject;

            var canvas = Object.FindObjectOfType<Canvas>();

            if (!canvas)
                canvas = new GameObject("Canvas").AddComponent<Canvas>();

            GameObjectUtility.SetParentAndAlign(go, canvas.gameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;

            rg.material = Object.Instantiate(rg.material);
        }
#endif

        public static UIVertex Set(this UIVertex vertex, float uvX, float uvY, Vector2 posX, Vector2 posY) {
            vertex.uv0 = new Vector2(uvX, uvY);
            vertex.position = new Vector2(posX.x, posY.y);
            return vertex;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(RoundedGraphic))]
    public class PixelPerfectShaderDrawer : PEGI_Inspector<RoundedGraphic> { }
#endif

}