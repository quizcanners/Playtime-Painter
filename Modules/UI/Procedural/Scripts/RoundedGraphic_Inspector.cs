using static QuizCanners.Inspect.pegi;
using QuizCanners.Utils;
using System.Collections.Generic;
using UnityEngine;
using QuizCanners.Inspect;

namespace PlaytimePainter.UI
{
    public partial class RoundedGraphic : IPEGI
    {

        private static List<Shader> _compatibleShaders;

        private static List<Shader> CompatibleShaders
        {
            get
            {
                if (_compatibleShaders == null)
                {
                    _compatibleShaders = new List<Shader>()
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Lit Button"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Box"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Unlinked/Box Unlinked"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Pixel Perfect"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Outline"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Unlinked/Outline Unlinked"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Button With Shadow"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Shadow"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Glow"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Gradient"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Unlinked/Gradient Unlinked"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Preserve Aspect"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/SubtractiveGraphic"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Image"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Primitives/Pixel Line"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Primitives/Pixel Line With Shadow"))
                        .TryAdd(Shader.Find("Playtime Painter/UI/Rounded/Pixel Perfect Screen Space"));
                }

                return _compatibleShaders;
            }
        }

        private static List<Material> _compatibleMaterials = new List<Material>();

        [SerializeField] private bool _showModules;
        [SerializeField] private int _inspectedModule;
        public static RoundedGraphic inspected;

        private const string info =
            "Rounded Graphic component provides additional data to pixel perfect UI shaders. Those shaders will often not display correctly in the scene view. " +
            "Also they may be tricky at times so take note of all the warnings and hints that my show in this inspector. " +
            "When Canvas is set To ScreenSpace-Camera it will also provide adjustive softening when scaled";

        internal static bool ClickDuplicate(ref Material mat, string newName = null, string folder = "Materials") =>
            ClickDuplicate(ref mat, folder, ".mat", newName);

        internal static bool ClickDuplicate<T>(ref T obj, string folder, string extension, string newName = null) where T : Object
        {

            if (!obj) return false;

            var changed = pegi.ChangeTrackStart();

#if UNITY_EDITOR
            var path = UnityEditor.AssetDatabase.GetAssetPath(obj);
            if (icon.Copy.ClickConfirm("dpl" + obj + "|" + path, "{0} Duplicate at {1}".F(obj, path)))
            {
                obj = QcUnity.Duplicate(obj, folder, extension: extension, newName: newName);
            }
#else
             if (icon.Copy.Click("Create Instance of {0}".F(obj)))
                obj = GameObject.Instantiate(obj);

#endif


            return changed;
        }

        public void Inspect()
        {
            inspected = this;

            FullWindow.DocumentationClickOpen(info, "About Rounded Graphic").nl();

            var mat = material;

            var can = canvas;

            var shad = mat.shader;

            var changed = ChangeTrackStart();

            bool expectedScreenPosition = false;

            bool expectedAtlasedPosition = false;

            if (!_showModules)
            {

                bool gotPixPerfTag = false;

                bool mayBeDefaultMaterial = true;

                bool expectingPosition = false;

                bool possiblePositionData = false;

                bool possibleFadePosition = false;

                bool needThirdUv;

                #region Material Tags 
                if (mat)
                {
                    var pixPfTag = mat.Get(ShaderTags.PixelPerfectUi);

                    gotPixPerfTag = !pixPfTag.IsNullOrEmpty();

                    if (!gotPixPerfTag)
                        "{0} doesn't have {1} tag".F(shad.name, ShaderTags.PixelPerfectUi.GetNameForInspector()).writeWarning();
                    else
                    {

                        mayBeDefaultMaterial = false;

                        expectedScreenPosition = pixPfTag.Equals(ShaderTags.PixelPerfectUis.Position.GetNameForInspector());

                        if (!expectedScreenPosition)
                        {

                            expectedAtlasedPosition = pixPfTag.Equals(ShaderTags.PixelPerfectUis.AtlasedPosition.GetNameForInspector());

                            if (!expectedAtlasedPosition)
                                possibleFadePosition = pixPfTag.Equals(ShaderTags.PixelPerfectUis.FadePosition.GetNameForInspector());
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

                if (toggle(ref linked, icon.Link, icon.UnLinked))
                    LinkedCorners = linked;

                for (var i = 0; i < _roundedCorners.Length; i++)
                {
                    var crn = _roundedCorners[i];

                    if ("{0}".F(linked ? "Courners" : ((Corner)i).ToString()).edit(70, ref crn, 0, 1f).nl())
                        _roundedCorners[i] = crn;
                }

                nl();

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
                                if ("FIX".Click())
                                    LinkedCorners = true;
                            }
                        }
                        else
                        {
                            if (linked)
                            {
                                "Material expects edge data to be Unlinked".writeWarning();
                                if ("FIX".Click())
                                    LinkedCorners = false;
                            }
                        }
                    }
                }

                nl();

                QcUnity.RemoveEmpty(_compatibleMaterials);

                if (mat && gotPixPerfTag)
                    _compatibleMaterials.AddIfNew(mat);

                bool showingSelection = false;

                var cmpCnt = _compatibleMaterials.Count;
                if (cmpCnt > 0 && ((cmpCnt > 1) || (!_compatibleMaterials[0].Equals(mat))))
                {

                    showingSelection = true;

                    if (select(ref mat, _compatibleMaterials, allowInsert: !mayBeDefaultMaterial))
                        material = mat;
                }

                if (mat)
                {

                    if (!Application.isPlaying)
                    {
                        var path = QcUnity.GetAssetFolder(mat);
                        if (path.IsNullOrEmpty())
                        {
                            nl();
                            "Material is not saved as asset. Click COPY next to it to save as asset. Or Click 'Refresh' to find compatible materials in your assets ".writeHint();
                            nl();
                        }
                        else
                            mayBeDefaultMaterial = false;
                    }

                    if (!showingSelection && !mayBeDefaultMaterial)
                    {
                        var n = mat.name;
                        if ("Rename Material".editDelayed("Press Enter to finish renaming.", 120, ref n))
                            QcUnity.RenameAsset(mat, n);
                    }
                }

                var changedMaterial = edit_Property(() => m_Material, this, fieldWidth: 60);
               

                if (!Application.isPlaying && ClickDuplicate(ref mat, gameObject.name))
                {
                    material = mat;
                    changedMaterial = true;
                }

                if (changedMaterial)
                    _compatibleMaterials.AddIfNew(material);


                if (!Application.isPlaying && icon.Refresh.Click("Find All Compatible Materials in Assets"))
                    _compatibleMaterials = ShaderTags.PixelPerfectUi.GetTaggedMaterialsFromAssets();


                nl();

                if (mat && !mayBeDefaultMaterial)
                {

                    if ("Shader".select(60, ref shad, CompatibleShaders, false, true))
                        mat.shader = shad;

                    var sTip = mat.Get(QuizCanners.Utils.ShaderTags.ShaderTip);

                    if (!sTip.IsNullOrEmpty())
                        FullWindow.DocumentationClickOpen(sTip, "Tip from shader tag");

                    if (shad)
                        shad.ClickHighlight();

                    if (icon.Refresh.Click("Refresh compatible Shaders list"))
                        _compatibleShaders = null;
                }

                nl();

                "Color".edit_Property(90, () => color, this).nl();

               /* var col = color;
                if (edit(ref col).nl())
                    color = col;*/

                #region Position Data

                if (possiblePositionData || feedPositionData)
                {

                    "Position Data".toggleIcon(ref feedPositionData, true);

                    if (feedPositionData)
                    {
                        "Position: ".editEnum(60, ref _positionDataType);

                        FullWindow.DocumentationClickOpen("Shaders that use position data often don't look right in the scene view.", "Camera dependancy warning");

                        nl();
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

                                    "Fade out at".edit(ref faeOutUvPosition).nl();

                                    break;
                            }
                        }
                    }

                    nl();
                }

                if (gotPixPerfTag && feedPositionData)
                {
                    if (!possiblePositionData)
                        "Shader doesn't have any PixelPerfectUI Position Tags. Position updates may not be needed".writeWarning();
                    else
                    {
                        nl();
                        /*
                        if (rectTransform.pivot != Vector2.one * 0.5f)
                        {
                            "Pivot is expected to be in the center for position processing to work".writeWarning();
                            pegi.nl();
                            if ("Set Pivot to 0.5,0.5".Click().nl())
                                rectTransform.SetPivotTryKeepPosition(Vector2.one * 0.5f);
                        }

                        if (rectTransform.localScale != Vector3.one)
                        {
                            "Scale deformation can interfear with some shaders that use position".writeWarning();
                            pegi.nl();
                            if ("Set local scale to 1".Click().nl())
                                rectTransform.localScale = Vector3.one;
                        }

                        if (rectTransform.localRotation != Quaternion.identity)
                        {
                            "Rotation can compromise calculations in shaders that need position".writeWarning();
                            if ("Reset Rotation".Click().nl())
                                rectTransform.localRotation = Quaternion.identity;

                        }*/
                    }

                    // if (_positionDataType == PositionDataType.AtlasPosition) {
                    //  "UV:".edit(ref atlasedUVs).nl();
                    //   pegi.edit01(ref atlasedUVs).nl();
                    // }

                }

                #endregion

                var spriteTag = mat ? mat.Get(ShaderTags.SpriteRole) : null;

                var noTag = spriteTag.IsNullOrEmpty();

                if (noTag || !spriteTag.SameAs(ShaderTags.SpriteRoles.Hide.GetNameForInspector()))
                {
                    if (noTag)
                        spriteTag = "Sprite";

                    spriteTag.edit_Property(90, () => sprite, this).nl();

                    /*;
                    if (spriteTag.edit(90, ref sp))
                        sprite = sp;*/
                    var sp = sprite;

                    if (sp)
                    {
                        var tex = sp.texture;

                        var rct = SpriteRect;

                        if (tex && (
                            !Mathf.Approximately(rct.width, rectTransform.rect.width)
                            || !Mathf.Approximately(rct.height, rectTransform.rect.height))
                                && icon.Size.Click("Set Native Size").nl())
                        {
                            rectTransform.sizeDelta = SpriteRect.size;
                            this.SetToDirty();
                        }
                    }
                    nl();
                }

                pegi.edit_Property("Maskable", 90, () => maskable, this, includeChildren: true).nl();

              /*  var isMaskable = maskable;

                if ("Maskable".toggleIcon(ref isMaskable))
                    maskable = isMaskable;*/

                pegi.edit_Property("Raycast Target", 90, () => raycastTarget, this).nl();

                /*var rt = raycastTarget;
                if ("Click-able".toggleIcon(hint: "Is RayCast Target", ref rt))
                    raycastTarget = rt;*/

                nl();
            }

            if ("Modules".enter_List(_modules, ref _inspectedModule, ref _showModules).nl())
            {
                ConfigStd = Encode().CfgData;
                this.SetToDirty();
            }

            if (changed) {
                SetVerticesDirty();
                Debug.Log("Raounded Graphic Dirty");
            }
        }
    }


    [PEGI_Inspector_Override(typeof(RoundedGraphic))] internal class PixelPerfectShaderDrawer : PEGI_Inspector_Override { }
}
