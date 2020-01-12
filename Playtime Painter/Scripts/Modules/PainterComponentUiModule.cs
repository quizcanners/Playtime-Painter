using System.Collections.Generic;
using UnityEngine;
using QuizCannersUtilities;
using UnityEngine.UI;
using PlayerAndEditorGUI;

namespace PlaytimePainter.ComponentModules {

    [TaggedType(Tag)]
    public class PainterComponentUiModule : ComponentModuleBase
    {
        private const string Tag = "UiMdl";
        public override string ClassTag => Tag;

        private static readonly ShaderProperty.TextureValue textureName = new ShaderProperty.TextureValue("_UiSprite");

        private Sprite GetSprite(ShaderProperty.TextureValue field) 
            => field.Equals(textureName) ? GetSprite() : null;
    
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
            var sp = GetSprite(field);

            if (!sp)
                return false;
            
            tex = sp.texture;

            return true;
        }

        public override void GetNonMaterialTextureNames(ref List<ShaderProperty.TextureValue> dest)
        {
            var sp = GetSprite();

            if (sp)
                dest.Add(textureName);
        }

        public override bool UpdateTilingFromMaterial(ShaderProperty.TextureValue field) {

            var sp = GetSprite(field);
            if (sp) {

                var id = painter.TexMeta;
                if (id == null)
                    return true;

                Rect uv = sp.TryGetAtlasedAtlasedUvs();

                id.tiling = Vector2.one/uv.size;
                id.offset = uv.position;

                return true;
            }

            return false;
        }
    }
}