using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;
using UnityEngine.UI;

namespace PlaytimePainter.ComponentModules {

    [TaggedType(Tag)]
    public class PainterComponentUiModule : ComponentModuleBase
    {
        private const string Tag = "UiMdl";
        public override string ClassTag => Tag;

        private static readonly ShaderProperty.TextureValue textureName = new ShaderProperty.TextureValue("_UiSprite");

        private Sprite GetSprite(ShaderProperty.TextureValue field) 
            => field.Equals(textureName) ? GetSprite() : null;

        private Texture GetTexture()
        {
            if (painter.IsUiGraphicPainter)
            {
                var image = painter.uiGraphic as Image;
                if (image)
                {
                    return image.sprite ? image.sprite.texture : null;
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

        public override bool BrushConfigPEGI()
        {
            var changed = false;
            var p = PlaytimePainter.inspected;
            
            if (p.IsUiGraphicPainter) {

                var gr = p.uiGraphic;

                if (!gr.raycastTarget) {
                    "Raycast target on UI is disabled, enable for painting".writeWarning();
                    if ("Enable Raycasts on UI".Click().nl())
                        gr.raycastTarget = true;
                }

                if (p.TexMeta.TargetIsRenderTexture()) {

                    var prop = p.GetMaterialTextureProperty;

                    if (textureName.Equals(prop) && p.NotUsingPreview)
                        "Image element can't use Render Texture as sprite. Switch to preview. Sprite will be updated when switching to CPU"
                            .writeWarning();

                    if (GlobalBrush.GetBrushType(false) == BrushTypes.Sphere.Inst)
                        "Brush sphere doesn't work with UI.".writeWarning();
                }

            }

            return changed;
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

                id.tiling = Vector2.one/uv.size;
                id.offset = uv.position;

                return true;
            }

            var raw = painter.uiGraphic as RawImage;
            if (raw)
            {
                id.tiling = raw.uvRect.max;
                id.offset = raw.uvRect.min;
                
                return true;
            }

            return false;
        }
    }
}