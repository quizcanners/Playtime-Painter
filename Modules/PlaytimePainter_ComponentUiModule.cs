using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace PainterTool.ComponentModules {

    [TaggedTypes.Tag(CLASS_KEY)]
    internal class PainterComponentUiModule : ComponentModuleBase
    {
        private const string CLASS_KEY = "UiMdl";
        public override string ClassTag => CLASS_KEY;

        private static readonly ShaderProperty.TextureValue textureName = new ShaderProperty.TextureValue("_UiSprite");

        public override string ToString() => "UI";

        private Sprite GetSprite(ShaderProperty.TextureValue field) 
            => field.Equals(textureName) ? GetSprite() : null;

        private Texture GetTexture()
        {
            if (painter.IsUiGraphicPainter)
            {
                var image = painter.uiGraphic as Image;
                if (image)
                {
                    var sprite = image.sprite;
                    return sprite ? sprite.texture : null;
                }

                var raw = painter.uiGraphic as RawImage;
                if (raw)
                    return raw.mainTexture;
            }

            return null;

           
        }

        private Sprite GetSprite() {

            if (painter.IsUiGraphicPainter) {
                var image = painter.uiGraphic as Image;
                if (image)
                    return image.sprite;
            }

            return null;
        }

        public override void BrushConfigPEGI()
        {
            var p = PainterComponent.inspected;
            
            if (p.IsUiGraphicPainter) {

                var gr = p.uiGraphic;

                if (!gr.raycastTarget) {
                    "Raycast target on UI is disabled, enable for painting".PL().WriteWarning();
                    if ("Enable Raycasts on UI".PL().Click().Nl())
                        gr.raycastTarget = true;
                }

                if (p.TexMeta.TargetIsRenderTexture()) {

                    var prop = p.GetMaterialTextureProperty();

                    if (textureName.Equals(prop) && p.NotUsingPreview)
                        "Image element can't use Render Texture as sprite. Switch to preview. Sprite will be updated when switching to CPU".PL().WriteWarning();

                    if (GlobalBrush.GetBrushType(TexTarget.RenderTexture) == BrushTypes.Sphere.Inst)
                        "Brush sphere doesn't work with UI.".PL().WriteWarning();
                }
            }
        }

        public override bool GetTexture(ShaderProperty.TextureValue field, ref Texture tex)
        {
            if (!field.Equals(textureName))
                return false;

            var tmp = GetTexture();

            if (!tmp)
                return false;
            
            tex = tmp;

            return true;
        }

        public override void GetNonMaterialTextureNames(ref List<ShaderProperty.TextureValue> dest)
        {
            var sp = GetTexture();

            if (sp)
                dest.Add(textureName);
        }

        public override bool UpdateTilingFromMaterial(ShaderProperty.TextureValue field) {

            if (!painter.IsUiGraphicPainter)
                return false;

            var id = painter.TexMeta;
            if (id == null)
                return true;

            var sp = GetSprite(field);
            if (sp) {

                Rect uv = sp.TryGetAtlasedAtlasedUvs();

                id.Tiling = Vector2.one/uv.size;
                id.Offset = uv.position;

                return true;
            }

            var raw = painter.uiGraphic as RawImage;
            if (raw)
            {
                var uvRect = raw.uvRect;
                id.Tiling = uvRect.max;
                id.Offset = uvRect.min;
                
                return true;
            }

            return false;
        }
    }
}