using System;
using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter
{

#if UNITY_2019_1_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif
    public class RoundedGraphic : Image, IKeepMyCfg, IPEGI,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {

        #region Rounded Corners

        [SerializeField] private float[] _roundedCorners = new float[1];

        public enum Corner { Down_Left = 0, Up_Left = 1, Up_Right = 2, Down_Right = 3 }


        public void SetCorner(Corner crn, float value) => SetCorner((int)crn, value);

        public void SetCorner(bool upper, bool right, float value) => SetCorner(upper ? (right ? 2 : 1) : (right ? 3 : 0), value);

        public void SetCorner(int index, float value)
        {

            index %= _roundedCorners.Length;

            if (_roundedCorners[index] != value)
            {
                _roundedCorners[index] = value;
                SetVerticesDirty();
            }

        }


        public float GetCorner(Corner crn) => GetCorner((int)crn);

        public float GetCorner(bool upper, bool right) => GetCorner(upper ? (right ? 2 : 1) : (right ? 3 : 0));
        
        public float GetCorner(int index) => _roundedCorners[index % _roundedCorners.Length];


        private void SetAllCorners(float value)
        {

            bool changed = false;

            for (int i = 0; i < _roundedCorners.Length; i++)
            {
                if (_roundedCorners[i] != value)
                {
                    _roundedCorners[i] = value;
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
                if (min.x != value)
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
                if (max.x != value)
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
                if (min.y != value)
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
                if (max.y != value)
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


                        pos = RectTransformUtility.WorldToScreenPoint(
                            IsOverlay ? null : (canvas ? canvas.worldCamera : null), rt.position);

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

        #region Inspector

        private static List<Shader> _compatibleShaders;

        private static List<Shader> CompatibleShaders =>
            _compatibleShaders ?? (_compatibleShaders = new List<Shader>()
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Lit Button"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Box"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Unlinked/Box Unlinked"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Pixel Perfect"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Outline"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Unlinked/Outline Unlinked"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Button With Shadow"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Shadow"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Gradient"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Unlinked/Gradient Unlinked"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Preserve Aspect"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/SubtractiveGraphic"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Image"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Primitives/Pixel Line"))
                .TryAdd(Shader.Find("Playtime Painter/UI/Primitives/Pixel Line With Shadow")));

        private static List<Material> _compatibleMaterials = new List<Material>();

        [SerializeField] private bool _showModules;
        [SerializeField] private int _inspectedModule;
        public static RoundedGraphic inspected;

        private const string info =
            "Rounded Graphic component provides additional data to pixel perfect UI shaders. Those shaders will often not display correctly in the scene view. " +
            "Also they may be tricky at times so take note of all the warnings and hints that my show in this inspector. " +
            "When Canvas is set To ScreenSpace-Camera it will also provide adjustive softening when scaled";

        public static bool ClickDuplicate(ref Material mat, string newName = null, string folder = "Materials") => 
            ClickDuplicate(ref mat, folder, ".mat", newName);

        public static bool ClickDuplicate<T>(ref T obj, string folder, string extension, string newName = null) where T : Object
        {

            if (!obj) return false;

            var changed = false;

#if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(obj);
            if (icon.Copy.ClickConfirm("dpl" + obj + "|" + path, "{0} Duplicate at {1}".F(obj, path)).changes(ref changed))
            {
                obj = QcUnity.Duplicate(obj, folder, extension: extension, newName: newName);
            }
#else
             if (icon.Copy.Click("Create Instance of {0}".F(obj)))
                obj = GameObject.Instantiate(obj);

#endif


            return changed;
        }

        public bool Inspect()
        {
            inspected = this;

            pegi.toggleDefaultInspector(this);

            pegi.FullWindowService.fullWindowDocumentationClickOpen(info, "About Rounded Graphic").nl();

            var mat = material;

            var can = canvas;

            var shad = mat.shader;

            var changed = false;

            var expectedScreenPosition = false;

            var expectedAtlasedPosition = false;

            if (!_showModules)
            {

                bool gotPixPerfTag = false;

                bool mayBeDefaultMaterial = true;

                bool expectingPosition = false;

                bool possiblePositionData = false;

                bool possibleFadePosition = false;

                bool needThirdUv = false;

                #region Material Tags 
                if (mat)
                {
                    var pixPfTag = mat.Get(ShaderTags.PixelPerfectUi);

                    gotPixPerfTag = !pixPfTag.IsNullOrEmpty();

                    if (!gotPixPerfTag)
                        "{0} doesn't have {1} tag".F(shad.name, ShaderTags.PixelPerfectUi.NameForDisplayPEGI()).writeWarning();
                    else
                    {

                        mayBeDefaultMaterial = false;

                        expectedScreenPosition = pixPfTag.Equals(ShaderTags.PixelPerfectUis.Position.NameForDisplayPEGI());

                        if (!expectedScreenPosition)
                        {

                            expectedAtlasedPosition = pixPfTag.Equals(ShaderTags.PixelPerfectUis.AtlasedPosition.NameForDisplayPEGI());

                            if (!expectedAtlasedPosition)
                                possibleFadePosition = pixPfTag.Equals(ShaderTags.PixelPerfectUis.FadePosition.NameForDisplayPEGI());
                        }

                        needThirdUv = expectedAtlasedPosition || (possibleFadePosition && feedPositionData);

                        expectingPosition = expectedAtlasedPosition || expectedScreenPosition;

                        possiblePositionData = expectingPosition || possibleFadePosition;

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

                            if (possiblePositionData && feedPositionData)
                            {
                                if ((can.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord2) == 0)
                                {
                                    "Material requires Canvas to pass Position Data trough Texcoord2 channel"
                                        .writeWarning();
                                    if ("Fix Canvas ".Click().nl())
                                        can.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
                                }
                                else if (needThirdUv && (can.additionalShaderChannels & AdditionalCanvasShaderChannels.TexCoord3) == 0)
                                {

                                    "Material requires Canvas to pass Texoord3 channel".writeWarning();
                                    if ("Fix Canvas".Click().nl())
                                        can.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord3;
                                }

                            }

                            if (can.renderMode == RenderMode.WorldSpace)
                            {
                                "Rounded UI isn't always working on world space UI yet.".writeWarning();
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

                for (var i = 0; i < _roundedCorners.Length; i++)
                {
                    var crn = _roundedCorners[i];

                    if ("Corner{0}".F(linked ? "s" : (" " + i)).edit(70, ref crn, 0, 1f).nl(ref changed))
                        _roundedCorners[i] = crn;
                }

                pegi.nl();

                if (mat)
                {
                    var needLink = ShaderTags.PerEdgeData.Get(mat);
                    if (!needLink.IsNullOrEmpty())
                    {
                        if (ShaderTags.PerEdgeRoles.LinkedCourners.Equals(needLink))
                        {
                            if (!linked)
                            {
                                "Material expects edge data to be linked".writeWarning();
                                if ("FIX".Click(ref changed))
                                    LinkedCorners = true;
                            }
                        }
                        else
                        {
                            if (linked)
                            {
                                "Material expects edge data to be Unlinked".writeWarning();
                                if ("FIX".Click(ref changed))
                                    LinkedCorners = false;
                            }
                        }
                    }
                }

                pegi.nl();

                QcUnity.RemoveEmpty(_compatibleMaterials);

                if (mat && gotPixPerfTag)
                    _compatibleMaterials.AddIfNew(mat);

                bool showingSelection = false;

                var cmpCnt = _compatibleMaterials.Count;
                if (cmpCnt > 0 && ((cmpCnt > 1) || (!_compatibleMaterials[0].Equals(mat))))
                {

                    showingSelection = true;

                    if (pegi.select(ref mat, _compatibleMaterials, allowInsert: !mayBeDefaultMaterial))
                        material = mat;
                }

                if (mat)
                {

                    if (!Application.isPlaying)
                    {
                        var path = QcUnity.GetAssetFolder(mat);
                        if (path.IsNullOrEmpty())
                        {
                            pegi.nl();
                            "Material is not saved as asset. Click COPY next to it to save as asset. Or Click 'Refresh' to find compatible materials in your assets ".writeHint();
                            pegi.nl();
                        }
                        else
                            mayBeDefaultMaterial = false;
                    }

                    if (!showingSelection && !mayBeDefaultMaterial)
                    {
                        var n = mat.name;
                        if ("Rename Material".editDelayed("Press Enter to finish renaming.", 120, ref n))
                            mat.RenameAsset(n);
                    }
                }

                if (pegi.edit(ref mat, 60).changes(ref changed) ||
                    (!Application.isPlaying && ClickDuplicate(ref mat, gameObject.name).changes(ref changed)))
                {
                    material = mat;
                    if (mat)
                        _compatibleMaterials.AddIfNew(mat);
                }

                if (!Application.isPlaying && icon.Refresh.Click("Find All Compatible Materials in Assets"))
                    _compatibleMaterials = ShaderTags.PixelPerfectUi.GetTaggedMaterialsFromAssets();


                pegi.nl();

                if (mat && !mayBeDefaultMaterial)
                {

                    if (shad)
                        shad.ClickHighlight();

                    if ("Shaders".select(60, ref shad, CompatibleShaders, false, true).changes(ref changed))
                        mat.shader = shad;

                    var sTip = mat.Get(QuizCannersUtilities.ShaderTags.ShaderTip);

                    if (!sTip.IsNullOrEmpty())
                        pegi.FullWindowService.fullWindowDocumentationClickOpen(sTip, "Tip from shader tag");

                    if (icon.Refresh.Click("Refresh compatible Shaders list"))
                        _compatibleShaders = null;
                }

                pegi.nl();

                var col = color;
                if (pegi.edit(ref col).nl(ref changed))
                    color = col;

                #region Position Data

                if (possiblePositionData || feedPositionData)
                {

                    "Position Data".toggleIcon(ref feedPositionData, true).changes(ref changed);

                    if (feedPositionData)
                    {
                        "Position: ".editEnum(60, ref _positionDataType).changes(ref changed);

                        pegi.FullWindowService.fullWindowDocumentationClickOpen("Shaders that use position data often don't look right in the scene view.", "Camera dependancy warning");

                        pegi.nl();
                    }
                    else if (expectingPosition)
                        "Shader expects Position data".writeWarning();

                    if (gotPixPerfTag)
                    {

                        if (feedPositionData)
                        {

                            switch (_positionDataType)
                            {
                                case PositionDataType.ScreenPosition:

                                    if (expectedAtlasedPosition)
                                        "Shader is expecting Atlased Position".writeWarning();

                                    break;
                                case PositionDataType.AtlasPosition:
                                    if (expectedScreenPosition)
                                        "Shader is expecting Screen Position".writeWarning();
                                    else if (sprite && sprite.packed)
                                    {
                                        if (sprite.packingMode == SpritePackingMode.Tight)
                                            "Tight Packing is not supported by rounded UI".writeWarning();
                                        else if (sprite.packingRotation != SpritePackingRotation.None)
                                            "Packing rotation is not supported by Rounded UI".writeWarning();
                                    }

                                    break;
                                case PositionDataType.FadeOutPosition:

                                    "Fade out at".edit(ref faeOutUvPosition).nl(ref changed);

                                    break;
                            }
                        }
                    }

                    pegi.nl();
                }

                if (gotPixPerfTag && feedPositionData)
                {
                    if (!possiblePositionData)
                        "Shader doesn't have any PixelPerfectUI Position Tags. Position updates may not be needed".writeWarning();
                    else
                    {
                        pegi.nl();
                        /*
                        if (rectTransform.pivot != Vector2.one * 0.5f)
                        {
                            "Pivot is expected to be in the center for position processing to work".writeWarning();
                            pegi.nl();
                            if ("Set Pivot to 0.5,0.5".Click().nl(ref changed))
                                rectTransform.SetPivotTryKeepPosition(Vector2.one * 0.5f);
                        }

                        if (rectTransform.localScale != Vector3.one)
                        {
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

                        }*/
                    }

                    // if (_positionDataType == PositionDataType.AtlasPosition) {
                    //  "UV:".edit(ref atlasedUVs).nl(ref changed);
                    //   pegi.edit01(ref atlasedUVs).nl(ref changed);
                    // }

                }

                #endregion

                var rt = raycastTarget;
                if ("Click-able".toggleIcon("Is RayCast Target", ref rt).nl(ref changed))
                    raycastTarget = rt;

                if (rt)
                    "On Click".edit_Property(() => OnClick, this).nl(ref changed);

                var spriteTag = mat ? mat.Get(ShaderTags.SpriteRole) : null;

                var noTag = spriteTag.IsNullOrEmpty();

                if (noTag || !spriteTag.SameAs(ShaderTags.SpriteRoles.Hide.NameForDisplayPEGI()))
                {

                    if (noTag)
                        spriteTag = "Sprite";

                    var sp = sprite;
                    if (spriteTag.edit(90, ref sp).changes(ref changed))
                        sprite = sp;

                    if (sp)
                    {

                        var tex = sp.texture;

                        var rct = SpriteRect;

                        if (tex && (rct.width != rectTransform.rect.width || rct.height != rectTransform.rect.height)
                                && icon.Size.Click("Set Native Size").nl())
                        {
                            rectTransform.sizeDelta = SpriteRect.size;
                            this.SetToDirty();
                        }


                    }

                    pegi.nl();

                }

             
            }

            if ("Modules".enter_List(ref _modules, ref _inspectedModule, ref _showModules).nl(ref changed))
                this.SaveCfgData();

            

            if (changed)
                SetVerticesDirty();

            return changed;
        }

        #endregion

        #region Mouse Mgmt

        public bool ClickPossible => MouseDown && ((Time.time - MouseDownTime) < maxHoldForClick);

        public UnityEvent OnClick;

        public float maxHoldForClick = 0.3f;
        public float maxMousePositionPixOffsetForClick = 20f;

        public bool MouseDown { get; private set; }
        public float MouseDownTime { get; private set; }
        public Vector2 MouseDownPosition { get; private set; }
        public bool MouseOver { get; private set; }

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

                var diff = MouseDownPosition - Input.mousePosition.ToVector2();

                if ((diff.magnitude) < maxMousePositionPixOffsetForClick)
                    OnClick.Invoke();
            }

            MouseDown = false;
        }

        #endregion

        #region Updates
        private Vector3 _previousPos = Vector3.zero;

        private void Update()
        {

            var needsUpdate = false;

            foreach (var m in _modules)
                needsUpdate |= m.Update(this);

            if (feedPositionData && rectTransform.position != _previousPos)
            {
                needsUpdate = true;
                _previousPos = rectTransform.position;
            }

            if (needsUpdate)
                SetAllDirty();
        }
        #endregion

        #region  Modules

        private List<RoundedButtonModuleBase> _modules = new List<RoundedButtonModuleBase>();

        [SerializeField] private string _modulesStd = "";

        public string ConfigStd
        {
            get { return _modulesStd; }
            set { _modulesStd = value; }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            this.LoadCfgData();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (!Application.isPlaying)
                this.SaveCfgData();
        }

        public CfgEncoder Encode() => new CfgEncoder()
            .Add_Abstract("mdls", _modules);

        public void Decode(string data) => this.DecodeTagsFrom(data);

        public bool Decode(string tg, string data)
        {

            switch (tg)
            {
                case "mdls": data.Decode_List(out _modules, RoundedButtonModuleBase.all); break;
                default: return false;
            }

            return true;
        }

        [TaggedType(Tag, "Uniform offset for stretched graphic", false)]
        protected class RoundedButtonStretchedUniformOffset : RoundedButtonModuleBase, IPEGI, IPEGI_ListInspect
        {

            private const string Tag = "StretchedOffset";

            public override string ClassTag => Tag;

            private float size = 100;

            #region Encode & Decode

            public override bool Decode(string tg, string data)
            {

                switch (tg)
                {
                    case "b": data.Decode_Base(base.Decode, this); break;
                    case "s": size = data.ToFloat(); break;
                    default: return false;
                }

                return true;
            }

            public override CfgEncoder Encode() => this.EncodeUnrecognized()
                .Add("b", base.Encode())
                .Add("s", size);

            #endregion

            #region Inspect
            public bool InspectInList(IList list, int ind, ref int edited)
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


                return false;
            }
            #endregion

        }

        [TaggedType(Tag, "Native Size from Tiled Texture", false)]
        protected class RoundedButtonNativeSizeForOverlayOffset : RoundedButtonModuleBase, IPEGI, IPEGI_ListInspect
        {

            private const string Tag = "TiledNatSize";

            private ShaderProperty.TextureValue referenceTexture = new ShaderProperty.TextureValue("_MainTex");

            public override string ClassTag => Tag;

            #region Inspect
            public bool InspectInList(IList list, int ind, ref int edited)
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

                return false;
            }
            public override bool Inspect()
            {
                var changed = base.Inspect();


                return changed;
            }
            #endregion

            #region Encode & Decode

            public override bool Decode(string tg, string data)
            {

                switch (tg)
                {

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

        [TaggedType(Tag, "Change Corners on Click", false)]
        protected class RoundedButtonCornersOnClick : RoundedButtonModuleBase, IPEGI, IPEGI_ListInspect
        {

            private const string Tag = "corners";

            public override string ClassTag => Tag;

            public float valueWhenOver = 0.5f;

            public float valueWhenOff = 0.5f;

            private readonly LinkedLerp.FloatValue _roundedCorners = new LinkedLerp.FloatValue();

            LerpData ld = new LerpData();

            public override bool Update(RoundedGraphic target)
            {

                ld.Reset();

                _roundedCorners.Portion(ld, target.MouseOver ? valueWhenOver : valueWhenOff);

                _roundedCorners.Lerp(ld);

                if (_roundedCorners.CurrentValue != target.GetCorner(0))
                {
                    target.SetAllCorners(_roundedCorners.CurrentValue);
                    return true;
                }

                return false;
            }

            #region Inspect
            public bool InspectInList(IList list, int ind, ref int edited)
            {

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
            #endregion

            #region Encode & Decode

            public override bool Decode(string tg, string data)
            {

                switch (tg)
                {
                    case "b": data.Decode_Base(base.Decode, this); break;
                    case "crn": _roundedCorners.Decode(data); break;
                    case "hov": valueWhenOver = data.ToFloat(); break;
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

      /*  protected class RoundedButtonModuleAttribute : AbstractWithTaggedTypes
        {
            public override TaggedTypesCfg TaggedTypes => RoundedButtonModuleBase.all;

            public RoundedButtonModuleAttribute() { }

            public RoundedButtonModuleAttribute(params System.Type[] types) : base(types) { }

        }

        [RoundedButtonModule]*/
        protected abstract class RoundedButtonModuleBase : AbstractKeepUnrecognizedCfg, IGotClassTag, IGotDisplayName
        {
            public static TaggedTypesCfg all = new TaggedTypesCfg(typeof(RoundedButtonModuleBase));
            public TaggedTypesCfg AllTypes => all;
            public abstract string ClassTag { get; }

            public virtual bool Update(RoundedGraphic target) => false;

            #region Inspect
            public virtual string NameForDisplayPEGI() => ClassTag;

            public override bool Inspect()
            {
                return false;
            }
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
            public static readonly ShaderTagValue FadePosition = new ShaderTagValue("FadePosition", PixelPerfectUi);
        }

        public static readonly ShaderTag SpriteRole = new ShaderTag("SpriteRole");

        public static class SpriteRoles
        {
            public static readonly ShaderTagValue Hide = new ShaderTagValue("Hide", SpriteRole);
            public static readonly ShaderTagValue Tile = new ShaderTagValue("Tile", SpriteRole);
            public static readonly ShaderTagValue Normal = new ShaderTagValue("Normal", SpriteRole);
        }

        public static readonly ShaderTag PerEdgeData = new ShaderTag("PerEdgeData");

        public static class PerEdgeRoles
        {
            public static readonly ShaderTagValue UnlinkedCourners = new ShaderTagValue("Unlinked", PerEdgeData);
            public static readonly ShaderTagValue LinkedCourners = new ShaderTagValue("Linked", PerEdgeData);
        }

    }

    public static class RoundedUiExtensions
    {

        public static void AddFull(this VertexHelper vh, UIVertex vert) =>
#if UNITY_2019_1_OR_NEWER
         vh.AddVert(vert.position, vert.color, vert.uv0, vert.uv1, vert.uv2, vert.uv3, vert.normal, vert.tangent);
#else
         vh.AddVert(vert.position, vert.color, vert.uv0, vert.uv1, vert.normal, vert.tangent);
#endif

#if UNITY_EDITOR

        [MenuItem("GameObject/UI/Playtime Painter/Invisible Raycat Target", false, 0)]
        private static void CreateInvisibleRaycastTarget()
        {
            var els = QcUnity.CreateUiElement<InvisibleUIGraphic>(Selection.gameObjects);

            foreach (var el in els)
            {
                el.name = "[]";
            }

        }

        [MenuItem("GameObject/UI/Playtime Painter/Rounded UI Graphic", false, 0)]
        private static void CreateRoundedUiElement()
        {
            QcUnity.CreateUiElement<RoundedGraphic>(Selection.gameObjects);
           /* bool createdForSelection = false;

            if (Selection.gameObjects.Length > 0)
            {

                foreach (var go in Selection.gameObjects)
                {
                    if (go.GetComponentInParent<Canvas>())
                    {
                        CreateRoundedUiElement(go);
                        createdForSelection = true;
                    }
                }
            }

            if (!createdForSelection)
            {

                var canvas = Object.FindObjectOfType<Canvas>();

                if (!canvas)
                    canvas = new GameObject("Canvas").AddComponent<Canvas>();

                CreateRoundedUiElement(canvas.gameObject);

            }
            */
        }
        
#endif

        public static UIVertex Set(this UIVertex vertex, float uvX, float uvY, Vector2 posX, Vector2 posY)
        {
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

        public override bool Inspect(Material mat)
        {

            var changed = pegi.toggleDefaultInspector(mat);

            mat.edit(Softness, "Softness", 0, 1).nl(ref changed);

            mat.edit(Outline).nl(ref changed);

            if (mat.IsKeywordEnabled(RoundedGraphic.UNLINKED_VERTICES))
                "UNLINKED VERTICES".nl();

            var go = QcUnity.GetFocusedGameObject();

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
    }

    #endregion
}