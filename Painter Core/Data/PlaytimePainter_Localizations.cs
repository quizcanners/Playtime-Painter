using System;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;
using static QuizCanners.Inspect.LazyLocalization;

namespace PainterTool {

    public enum MsgPainter {
        PreserveTransparency, RoundedGraphic,
        BrushType, BrushTypeNormal, BrushTypeDecal, BrushTypeLazy, BrushTypeSphere, BrushTypePixel,
        BlitMode, BlitModeAlpha, BlitModeAdd, BlitModeSubtract, BlitModeCopy, BlitModeMin, BlitModeMax, BlitModeBlur,
        BlitModeOff, BlitModeBloom, BlitModeProjector, BlitModeFiller, BlitModeCustom,
        LockToolToUseTransform, HideTransformTool, MeshProfileUsage, Flow, Scale, Sharpness, CopyFrom, TextureSettings, previewRGBA, AutoSelectMaterial,
         aboutDisableDocumentation, SampleColor, PreviewRecommended, AlphaBufferBlit, Opacity, SpreadSpeed, BlurAmount,
         Unnamed, TransparentLayer, PleaseSelect, MeshPoint, Vertex, MeshPointPositionTool

    }

    internal static class LazyLocalization 
    {

        private static readonly TranslationsEnum painterTranslations = new();

        public static LazyTranslation Get(this MsgPainter msg, int lang = 0) {

            int index = (int) msg;

            if (painterTranslations.Initialized(index))
                return painterTranslations.GetWhenInited(index, lang);

            switch (msg) {
                case MsgPainter.PreserveTransparency:
                    msg.Translate("Preserve Transparency", "if every pixel of texture has alpha = 1 (Max) Unity will be save it as .png without transparency. To counter this " +
                                                           " I set first pixels to alpha 0.9. I know it is hacky, it you know a better way, let me know"); break;
                case MsgPainter.BrushType:
                    msg.Translate("Brush Type");

                    break;

                case MsgPainter.BlitMode:
                    msg.Translate("Blit Mode");
                    break;

                case MsgPainter.LockToolToUseTransform:
                    msg.Translate("Lock texture to use transform tools. Or click 'Hide transform tool'");
                    break;

                case MsgPainter.HideTransformTool:
                    msg.Translate("Hide transform tool");
                    break;
              
                case MsgPainter.MeshProfileUsage:
                    msg.Translate("Mesh Profile usage", ("If using projected UV, place sharpNormal in TANGENT. {0}" +
                                                         "Vectors should be placed in normal and tangent slots to batch correctly.{0}" +
                                                         "Keep uv1 as is for baked light and damage shaders.{0}" +
                                                         "I place Shadows in UV2{0}" +
                                                         "I place Edge in UV3.{0}").F(pegi.EnvironmentNl));
                    break;
                case MsgPainter.Flow:
                    msg.Translate("Flow");
                    break;

                case MsgPainter.Scale:
                    msg.Translate("Scale");
                    break;
                case MsgPainter.Sharpness:
                    msg.Translate("Sharpness", "Makes edges more clear.");
                    break;
                case MsgPainter.CopyFrom:
                    msg.Translate("Copy From");
                    break;
                case MsgPainter.TextureSettings:
                    msg.Translate("Texture options");
                    break;
                case MsgPainter.previewRGBA:
                    msg.Translate("Preview Edited RGBA", 
                        "When using preview shader, only color channels you are currently editing will be visible in the preview. Useful when you want to edit only one color channel");
                    break;
                case MsgPainter.AutoSelectMaterial:
                    msg.Translate("Auto Select Materials", "As you paint, component will keep checking Sub Mesh index and will change painted material based on that index.");
                    break;
                case MsgPainter.aboutDisableDocumentation:
                    msg.Translate("Hide what?", 
                            "This is an example of what will be hidden if you toggle this option (This blue question mark icons)")
                    ;
                    break;
                case MsgPainter.SampleColor:
                    msg.Translate("Sampling Texture colors",
                        "To sample source color of the texture, hold Ctrl before clicking Left Mouse Button");
                    break;
                case MsgPainter.PreviewRecommended:
                    msg.Translate("Preview is recommended",
                        "It is recommended to use preview when using Alpha Blit as it will improve performance " +
                        "and enable brush transparency option.");
                    break;
                case MsgPainter.AlphaBufferBlit:
                    msg.Translate("AlphaBufferBlit",
                        "Will render brush to Alpha Buffer first and then use that Alpha buffer to render changes to texture. For Sphere brush helps avoid many various artifacts." +
                        "Using Preview will improve performance, as it will not apply changes to texture until you exit preview mode, or change any setting that affects blit mode. " +
                        "Using it without Preview will result in decreased performance in comparison to disabled Alpha Buffer as it will need to update original texture every frame to " +
                        "allow you to see the changes" +
                        "Please report any issues you encounter while using this, as this is a new feature, and there are planty of places where it can function not as desired. "
                        

                    );
                    break;
                case MsgPainter.Opacity:
                    msg.Translate("Opacity");
                    break;
                case MsgPainter.SpreadSpeed:
                    msg.Translate("Spread");
                    break;
                case MsgPainter.BlurAmount:
                    msg.Translate("Radius");

                    break;

                #region Brush Types
                case MsgPainter.BrushTypeNormal:
                    msg.Translate("Normal", "Regular round texture space brush");
                    break;

                case MsgPainter.BrushTypeDecal:
                    msg.Translate("Decal ", "Paints volumetric decals. It uses alpha channel of the painted texture as height. Denting decals (think bullet holes)" +
                                            "will subtract alpha if their depth is higher (deeper) and paint their color. Additive decals will add alpha if theirs is higher. ");
                    break;

                case MsgPainter.BrushTypeLazy:
                    msg.Translate("Lazy ", "Lazy brush will follow your mouse with a bit of a delay. It tries to paint a smooth line." +
                                           " Also useful if you want to paint a semi-transparent line.");
                    break;

                case MsgPainter.BrushTypeSphere:
                    msg.Translate("Sphere ", "Sphere brush is very different from all other brushes. It uses world position to paint. " +
                                             "It is perfect for working with complex meshes. It can even paint on animated skinned meshes.");
                    break;

                case MsgPainter.BrushTypePixel:
                    msg.Translate("Pixel ", "Paints square pixel perfect shape. Recommended to use with Preview shader.");
                    break;

                #endregion

                #region Blit Modes

                case MsgPainter.BlitModeAlpha:
                    msg.Translate("Alpha Blit", "The most standard brush. It will gradually replace existing color with the color you are painting with. " +
                                                "Keep in mind, if you are painting on texture with transparency (has areas you can see trough), also toggle Transparent Blit mode. " +
                                                "Otherwise you'll see some weird outlines.");
                    break;

                case MsgPainter.BlitModeAdd:
                    msg.Translate("Add", "Adds brush color to texture color.");
                    break;

                case MsgPainter.BlitModeSubtract:
                    msg.Translate("Subtract", "Subtracts brush color from texture color.");
                    break;

                case MsgPainter.BlitModeCopy:
                    msg.Translate("Copy ", "Copies pixels from selected source texture to painted texture.");
                    break;

                case MsgPainter.BlitModeMin:
                    msg.Translate("Min ", "Paints smallest value between brush color and current texture color for each channel.");
                    break;

                case MsgPainter.BlitModeMax:
                    msg.Translate("Max ", "Paints highest value between brush color and current texture color for each channel.");
                    break;

                case MsgPainter.BlitModeBlur:
                    msg.Translate("Blur ", "Applies blur effect. Mixes each pixel's color with the color of pixels next to it.");
                    break;

                case MsgPainter.
                    BlitModeOff:
                    msg.Translate("Pixel Reshape ", "This one is more in the experimental category. It writes distance from central pixel. Could be used to create texture with pixels shaped as hexagons, or bricks in the wall. "
                        );
                    break;

                case MsgPainter.BlitModeBloom:
                    msg.Translate(
                        "Bloom ", 
                        "Similar to Blur, but instead of blurring the colors, spreads brightness from bright pixels to darker ones");
                    break;

                case MsgPainter.BlitModeProjector:
                    msg.Translate("Projection ", 
                        "Will create a camera that will serve as a projector. This mode is similar to Copy, but instead of matching UV coordinates of source and target it will sample source " +
                        "using projector. Only World Space brushes can use this Blit Mode. Currently only sphere brush is a world space brush. First step is usually to position projector camera. "
                        );
                    break;

                case MsgPainter.BlitModeFiller:
                    msg.Translate("Ink Filler ", 
                        " Inspired by comic books. After you paint BLACK lines, this brush will try to gradually fill the painted area with color without crossing those lines. ");
                    break;
                case MsgPainter.BlitModeCustom:
                    msg.Translate("Custom", "Plug your own shader"); 
                    break;
                case MsgPainter.Unnamed:
                    msg.Translate("Unnamed ",
                        "The selected class doesn't have a Readable name.");
                    break;
                #endregion
                case MsgPainter.TransparentLayer:
                    msg.Translate("Transparent",
                            "Toggle this ON if texture has transparent areas which will not be visible. This will affect how they are painted: color of the transparent areas will be neglected to avoid outline artifacts.")
                        ;
                    break;
                case MsgPainter.PleaseSelect:
                    msg.Translate("Painter tool is not selected (Select it in the top left area)");
                    break;


                case MsgPainter.MeshPoint:
                    msg.Translate("Mesh Point", "Mesh Point contain a number of vertices which share the same position");
                    break;
                case MsgPainter.Vertex:
                    msg.Translate("Vertex", "Each vertex can contain information like : position, UV sets, normal, tangent, color");
                    break;
                case MsgPainter.MeshPointPositionTool:
                    msg.Translate("VERTICES", ("LMB - Drag {1} {0} " +
                                               "Alt - Move {1} To Grid {0}" +
                                               "U - make Triangle unique. {0}" +
                                               "M - merge with nearest {1} while dragging {0}" +
                                               "This tool also contains functionality related to smoothing and sharpening of the edges.")
                        .F(pegi.EnvironmentNl, MsgPainter.MeshPoint.GetText()));
                    break;

                case MsgPainter.RoundedGraphic:
                    msg.Translate("Rounded Graphic",
                        "Rounded Graphic component provides additional data to pixel perfect UI shaders. Those shaders will often not display correctly in the scene view. " +
                        "Also they may be tricky at times so take note of all the warnings and hints that my show in this inspector." +
                        "");
                    break;
            }

            return painterTranslations.GetWhenInited(index, lang);
        }




        
        public static void Write(this MsgPainter m) => m.GetText().PegiLabel(m.GetDescription()).Write(); 
        public static void Write(this MsgPainter m, int width) => m.GetText().PegiLabel(m.GetDescription(), width).Write(); 
        public static void Write(this MsgPainter m, string tip, int width) => m.GetText().PegiLabel(tip, width).Write(); 
        public static void Write(this MsgPainter m, string tip) { m.GetText().PegiLabel(tip).ApproxWidth().Write(); }

        public static string GetText(this MsgPainter msg)
        {
             var lt =   msg.Get();
             return lt != null ? lt.ToString() : msg.ToString();
        }

        public static string GetDescription(this MsgPainter msg)
        {
            var lt = msg.Get();
            return lt != null ? lt.details : msg.ToString();
        }

        private static void Translate(this MsgPainter smg, string english)
        {
            var org = painterTranslations[(int)smg];
            org[eng] = new LazyTranslation(english);
        }

        private static void Translate(this MsgPainter smg, string english, string englishDetails)
        {
            var org = painterTranslations[(int)smg];
            org[eng] = new LazyTranslation(english, englishDetails);
        }

        
        public static pegi.ChangesToken DocumentationClick(this MsgPainter msg) => new(!SO_PainterDataAndConfig.hideDocumentation && msg.Get().DocumentationClick());
        
        public static pegi.ChangesToken DocumentationWarning(this MsgPainter msg) => new(!SO_PainterDataAndConfig.hideDocumentation && msg.Get().WarningDocumentation());

    }
}