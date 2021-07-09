using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using QuizCanners.Migration;
using QuizCanners.Lerp;


namespace PlaytimePainter.UI
{

#if UNITY_2019_1_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif
    public partial class RoundedGraphic : Image, ICfg, 
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {

        #region Rounded Corners

        [SerializeField] private float[] _roundedCorners = new float[1];

        public enum Corner
        {
            Down_Left = 0,
            Up_Left = 1,
            Up_Right = 2,
            Down_Right = 3
        }

        public void SetCorner(Corner crn, float sharpness) => SetCorner((int) crn, sharpness);

        public void SetCorner(bool upper, bool right, float sharpness) =>
            SetCorner(upper ? (right ? 2 : 1) : (right ? 3 : 0), sharpness);

        public void SetCorner(int index, float sharpness)
        {

            index %= _roundedCorners.Length;

            if (Mathf.Approximately(_roundedCorners[index], sharpness) == false)
            {
                _roundedCorners[index] = sharpness;
                SetVerticesDirty();
            }

        }

        public float GetCorner(Corner crn) => GetCorner((int) crn);

        public float GetCorner(bool upper, bool right) => GetCorner(upper ? (right ? 2 : 1) : (right ? 3 : 0));

        public float GetCorner(int index) => _roundedCorners[index % _roundedCorners.Length];

        private void SetAllCorners(float sharpness)
        {

            bool changed = false;

            for (int i = 0; i < _roundedCorners.Length; i++)
            {
                if (Mathf.Approximately(_roundedCorners[i], sharpness) == false)
                {
                    _roundedCorners[i] = sharpness;
                    changed = true;
                }
            }

            if (changed)
                SetVerticesDirty();
        }

        public bool LinkedCorners
        {
            get { return _roundedCorners.Length == 1; }

            set
            {
                var targetValue = value ? 1 : 4;

                if (targetValue == _roundedCorners.Length) return;

                if (material)
                    material.SetShaderKeyword(UNLINKED_VERTICES, targetValue > 1);

                var tmp = _roundedCorners[0];

                _roundedCorners = new float[targetValue];

                for (var i = 0; i < _roundedCorners.Length; i++)
                    _roundedCorners[i] = tmp;
            }
        }

        #endregion

        #region Screen Position

        public bool feedPositionData = true;

        protected enum PositionDataType
        {
            ScreenPosition,
            AtlasPosition,
            FadeOutPosition
        }

        [SerializeField] protected PositionDataType _positionDataType = PositionDataType.ScreenPosition;

        public float FadeFromX
        {
            set
            {
                var min = faeOutUvPosition.min;
                if (Mathf.Approximately(min.x, value) == false)
                {
                    min.x = value;
                    faeOutUvPosition.min = min;
                    SetVerticesDirty();
                }
            }
        }

        public float FadeToX
        {
            set
            {
                var max = faeOutUvPosition.max;
                if (Mathf.Approximately(max.x, value) == false)
                {
                    max.x = value;
                    faeOutUvPosition.max = max;
                    SetVerticesDirty();
                }
            }
        }

        public float FadeFromY
        {
            set
            {
                Vector2 min = faeOutUvPosition.min;
                if (Mathf.Approximately(min.y, value) == false)
                {
                    min.y = value;
                    faeOutUvPosition.min = min;
                    SetVerticesDirty();
                }
            }
        }

        public float FadeToY
        {
            set
            {
                Vector2 max = faeOutUvPosition.max;
                if (Mathf.Approximately(max.y, value) == false)
                {
                    max.y = value;
                    faeOutUvPosition.max = max;
                    SetVerticesDirty();
                }
            }
        }

        [SerializeField] private Rect faeOutUvPosition = new Rect(0, 0, 1, 1);

        private Rect SpriteRect
        {
            get
            {

                var sp = sprite;

                if (!sp)
                    return Rect.MinMaxRect(0, 0, 100, 100);

                if (!Application.isPlaying)
                    return sp.rect;

                return (sp.packed && sp.packingMode != SpritePackingMode.Tight) ? sp.textureRect : sp.rect;
            }
        }

        #endregion

        #region Populate Mesh

        public const string UNLINKED_VERTICES = "_UNLINKED";
        public const string EDGE_SOFTNESS_FLOAT = "_Edges";

        private bool IsOverlay
        {
            get
            {
                var c = canvas;
                return c && (c.renderMode == RenderMode.ScreenSpaceOverlay || !c.worldCamera);
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {

            if (!gameObject.activeInHierarchy)
            {
                if (Debug.isDebugBuild && Application.isEditor)
                    Debug.LogError("On populate mesh is called for disabled UI element");

                return;
            }

            var rt = rectTransform;
            var piv = rt.pivot;
            var rectSize = rt.rect.size;


            vh.Clear();

            var vertex = UIVertex.simpleVert;


            var rctS = rectSize;

            float rectDiff = rctS.x - rctS.y;

            rctS = new Vector2(Mathf.Max(0, rectDiff / rctS.x), Mathf.Max(0, (-rectDiff) / rctS.y));

            var scaleToSided = rctS.x - rctS.y; // If x>0 - positive, else - negative


            if (feedPositionData)
            {

                var pos = Vector2.zero;

                switch (_positionDataType)
                {
                    case PositionDataType.AtlasPosition:

                        var sp = sprite;

                        if (sp)
                        {

                            var tex = sp.texture;

                            if (tex)
                            {

                                var texturePixelSize = new Vector2(tex.width, tex.height);

                                var atlased = SpriteRect;

                                pos = atlased.center / texturePixelSize;

                                vertex.uv3 = atlased.size / texturePixelSize;
                            }
                        }

                        break;
                    case PositionDataType.ScreenPosition:


                        var canvas1 = canvas;
                        pos = RectTransformUtility.WorldToScreenPoint(
                            IsOverlay ? null : (canvas1 ? canvas1.worldCamera : null), rt.position);

                        pos.Scale(new Vector2(1f / Screen.width, 1f / Screen.height));

                        break;

                    case PositionDataType.FadeOutPosition:

                        pos = faeOutUvPosition.min;

                        vertex.uv3 = faeOutUvPosition.max;

                        break;
                }

                vertex.uv2 = pos;

            }

            var corner1 = (Vector2.zero - piv) * rectSize;
            var corner2 = (Vector2.one - piv) * rectSize;

            vertex.color = color;

            vertex.uv0 = new Vector2(0, 0);
            vertex.uv1 = new Vector2(scaleToSided, GetCorner(0));
            vertex.position = new Vector2(corner1.x, corner1.y);
            vh.AddFull(vertex);

            vertex.uv0 = new Vector2(0, 1);
            vertex.uv1.y = GetCorner(1);
            vertex.position = new Vector2(corner1.x, corner2.y);
            vh.AddFull(vertex);

            vertex.uv0 = new Vector2(1, 1);
            vertex.uv1.y = GetCorner(2);
            vertex.position = new Vector2(corner2.x, corner2.y);
            vh.AddFull(vertex);

            vertex.uv0 = new Vector2(1, 0);
            vertex.uv1.y = GetCorner(3);
            vertex.position = new Vector2(corner2.x, corner1.y);
            vh.AddFull(vertex);

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

                // TODO: Implement atlasing for Unlinked

                var cornMid = (corner1 + corner2) * 0.5f;

                vertex.uv1.y = GetCorner(0);
                vh.AddFull(vertex.Set(0, 0.5f, corner1, cornMid)); //4
                vh.AddFull(vertex.Set(0.5f, 0, cornMid, corner1)); //5

                vertex.uv1.y = GetCorner(1);
                vh.AddFull(vertex.Set(0.5f, 1, cornMid, corner2)); //6
                vh.AddFull(vertex.Set(0, 0.5f, corner1, cornMid)); //7

                vertex.uv1.y = GetCorner(2);
                vh.AddFull(vertex.Set(1, 0.5f, corner2, cornMid)); //8
                vh.AddFull(vertex.Set(0.5f, 1, cornMid, corner2)); //9

                vertex.uv1.y = GetCorner(3);
                vh.AddFull(vertex.Set(0.5f, 0, cornMid, corner1)); //10
                vh.AddFull(vertex.Set(1, 0.5f, corner2, cornMid)); //11

                vertex.uv1.y = GetCorner(0);
                vh.AddFull(vertex.Set(0.5f, 0.5f, cornMid, cornMid)); //12

                vertex.uv1.y = GetCorner(1);
                vh.AddFull(vertex.Set(0.5f, 0.5f, cornMid, cornMid)); //13

                vertex.uv1.y = GetCorner(2);
                vh.AddFull(vertex.Set(0.5f, 0.5f, cornMid, cornMid)); //14

                vertex.uv1.y = GetCorner(3);
                vh.AddFull(vertex.Set(0.5f, 0.5f, cornMid, cornMid)); //15

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

        #endregion

        #region Mouse Mgmt

        internal bool ClickPossible => MouseDown && ((Time.time - MouseDownTime) < maxHoldForClick);

        //  public UnityEvent OnClick;

        internal float maxHoldForClick = 0.3f;
        internal float maxMousePositionPixOffsetForClick = 20f;

        internal bool MouseDown { get; private set; }
        internal float MouseDownTime { get; private set; }
        internal Vector2 MouseDownPosition { get; private set; }
        internal bool MouseOver { get; private set; }

        public void OnPointerEnter(PointerEventData eventData) => MouseOver = true;

        public void OnPointerExit(PointerEventData eventData)
        {
            MouseDown = false;
            MouseOver = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            MouseDownPosition = Input.mousePosition;
            MouseDown = true;
            MouseDownTime = Time.time;
        }

        public void OnPointerUp(PointerEventData eventData)
        {

            if (ClickPossible)
            {

               // var diff = MouseDownPosition - Input.mousePosition.ToVector2();

               /* if ((diff.magnitude) < maxMousePositionPixOffsetForClick)
                    OnClick.Invoke();*/
            }

            MouseDown = false;
        }

        #endregion

        #region Updates
        private Vector3 _previousPos = Vector3.zero;
        private Transform _previousParent;

        private void Update()
        {

            var needsUpdate = false;

            foreach (var m in _modules)
                needsUpdate |= m.Update(this);

            if (transform.parent != _previousParent || (feedPositionData && rectTransform.position != _previousPos))
                needsUpdate = true;
            
            if (needsUpdate)
            {
                SetAllDirty();
                _previousPos = rectTransform.position;
                _previousParent = transform.parent;
            }
        }
        #endregion

        #region  Modules

        private List<RoundedButtonModuleBase> _modules = new List<RoundedButtonModuleBase>();

        [SerializeField] private CfgData _modulesStd;

        public CfgData ConfigStd
        {
            get { return _modulesStd; }
            set
            {
                _modulesStd = value;
                this.SetToDirty();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            this.DecodeFull(ConfigStd);
            //this.LoadCfgData();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (!Application.isPlaying)
                ConfigStd = Encode().CfgData;
            //this.SaveCfgData();
        }

        public CfgEncoder Encode() => new CfgEncoder()
            .Add_Abstract("mdls", _modules);

        public void Decode(string key, CfgData data)
        {
            switch (key)
            {
                case "mdls": data.ToList(out _modules, RoundedButtonModuleBase.all); break;
            }
        }

        [TaggedType(CLASS_KEY, "Uniform offset for stretched graphic", false)]
        protected class RoundedButtonStretchedUniformOffset : RoundedButtonModuleBase, IPEGI, IPEGI_ListInspect
        {

            private const string CLASS_KEY = "StretchedOffset";

            public override string ClassTag => CLASS_KEY;

            private float size = 100;

            #region Encode & Decode

            public override void Decode(string key, CfgData data)
            {

                switch (key)
                {
                    case "s": size = data.ToFloat(); break;
                }
            }

            public override CfgEncoder Encode() => new CfgEncoder()//this.EncodeUnrecognized()
                .Add("b", base.Encode())
                .Add("s", size);

            #endregion

            #region Inspect
            public void InspectInList(ref int edited, int ind)
            {
                // var tg = inspected;

                var rt = inspected.rectTransform;

                if (rt.anchorMin != Vector2.zero || rt.anchorMax != Vector2.one)
                {

                    if ("Stretch Anchors".Click())
                    {
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                    }
                }
                else
                {

                    var offset = rt.offsetMin.x;

                    var rect = rt.rect;

                    if (icon.Refresh.Click("Refresh size ({0})".F(size)))
                        size = Mathf.Max(Mathf.Abs(rect.width), Mathf.Abs(rect.height));

                    if ("Offset".edit(ref offset, -size, size))
                    {
                        rt.offsetMin = Vector2.one * offset;
                        rt.offsetMax = -Vector2.one * offset;
                    }
                }
            }
            #endregion

        }

        [TaggedType(CLASS_KEY, "Native Size from Tiled Texture", false)]
        protected class RoundedButtonNativeSizeForOverlayOffset : RoundedButtonModuleBase, IPEGI, IPEGI_ListInspect
        {

            private const string CLASS_KEY = "TiledNatSize";

            private ShaderProperty.TextureValue referenceTexture = new ShaderProperty.TextureValue("_MainTex");

            public override string ClassTag => CLASS_KEY;

            #region Inspect
            public void InspectInList(ref int edited, int ind)
            {

                var mat = inspected.material;
                if (mat)
                {

                    pegi.select_or_edit_TextureProperty(ref referenceTexture, mat);

                    var tex = referenceTexture.Get(mat);

                    if (tex)
                    {
                        if (icon.Size.Click("Set Native Size for Texture, using it's Tile/Offset"))
                        {

                            var size = new Vector2(tex.width, tex.height);
                            var til = referenceTexture.GetTiling(mat);
                            size *= til;

                            inspected.rectTransform.sizeDelta = size;

                        }
                    }
                }
                else "No Material".write();

            }

            #endregion

            #region Encode & Decode

            public override void Decode(string key, CfgData data)
            {

                switch (key)
                {
                    case "b": data.Decode(base.Decode); break;
                    case "mp": referenceTexture.DecodeFull(data); break;
                }
            }

            public override CfgEncoder Encode() => new CfgEncoder()//this.EncodeUnrecognized()
                    .Add("b", base.Encode())
                    .Add("mp", referenceTexture);

            #endregion
        }

        [TaggedType(CLASS_KEY, "Change Corners on Click", false)]
        protected class RoundedButtonCornersOnClick : RoundedButtonModuleBase, IPEGI, IPEGI_ListInspect
        {

            private const string CLASS_KEY = "corners";

            public override string ClassTag => CLASS_KEY;

            public float valueWhenOver = 0.5f;

            public float valueWhenOff = 0.5f;

            private readonly LinkedLerp.FloatValue _roundedCorners = new LinkedLerp.FloatValue();

            private readonly LerpData ld = new LerpData();

            public override bool Update(RoundedGraphic target)
            {

                ld.Reset();

                _roundedCorners.Portion(ld, target.MouseOver ? valueWhenOver : valueWhenOff);

                _roundedCorners.Lerp(ld, canSkipLerp: false);

                if (!Mathf.Approximately(_roundedCorners.CurrentValue, target.GetCorner(0)))
                {
                    target.SetAllCorners(_roundedCorners.CurrentValue);
                    return true;
                }

                return false;
            }

            #region Inspect
            public void InspectInList(ref int edited, int ind)
            {

                "Normal".edit(50, ref valueWhenOff, 0, 1);

                "On Hover".edit(50, ref valueWhenOver, 0, 1).nl();

                if (icon.Enter.Click())
                    edited = ind;

            }

           public override void Inspect()
            {
                _roundedCorners.Nested_Inspect().nl();
            }
            #endregion

            #region Encode & Decode

            public override void Decode(string key, CfgData data)
            {

                switch (key)
                {
                    case "b": data.Decode(base.Decode); break;
                    case "crn": _roundedCorners.DecodeFull(data); break;
                    case "hov": valueWhenOver = data.ToFloat(); break;
                    case "nrm": valueWhenOff = data.ToFloat(); break;
                }
            }

            public override CfgEncoder Encode() => new CfgEncoder() //this.EncodeUnrecognized()
                    .Add("b", base.Encode())
                    .Add("crn", _roundedCorners)
                    .Add("hov", valueWhenOver)
                    .Add("nrm", valueWhenOff);
            #endregion
        }

        protected abstract class RoundedButtonModuleBase : IGotClassTag, ICfg, IGotReadOnlyName
        {
            public static TaggedTypesCfg all = new TaggedTypesCfg(typeof(RoundedButtonModuleBase));
            public TaggedTypesCfg AllTypes => all;
            public abstract string ClassTag { get; }

            public virtual bool Update(RoundedGraphic target) => false;

            #region Inspect
            public virtual string GetNameForInspector() => ClassTag;

            public virtual void Inspect()
            {
            }
            #endregion

            #region Encode & Decode
            public virtual CfgEncoder Encode() => new CfgEncoder();//this.EncodeUnrecognized();

            public virtual void Decode(string key, CfgData data)
            {

            }
            #endregion
        }

        #endregion
    }
}