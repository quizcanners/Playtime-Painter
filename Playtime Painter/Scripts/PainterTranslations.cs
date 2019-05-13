using PlaytimePainter;
using QuizCannersUtilities;
using UnityEditor;

namespace PlayerAndEditorGUI {

    public enum MsgPainter {
        PreserveTransparency,
        BrushType, BrushTypeNormal, BrushTypeDecal, BrushTypeLazy, BrushTypeSphere, BrushTypePixel,
        BlitMode, BlitModeAlpha, BlitModeAdd, BlitModeSubtract, BlitModeCopy, BlitModeMin, BlitModeMax, BlitModeBlur,
        BlitModeOff, BlitModeBloom, BlitModeProjector, BlitModeFiller,
        LockToolToUseTransform, HideTransformTool, AboutPlaytimePainter,
         MeshProfileUsage, Speed, Scale, Hardness, CopyFrom, TextureSettings, previewRGBA, AutoSelectMaterial,
         aboutDisableDocumentation, SampleColor, PreviewRecommended, AlphaBufferBlit, Opacity, SpreadSpeed, BlurAmount,
         Unnamed, TransparentLayer, PleaseSelect, MeshPoint, Vertex, MeshPointPositionTool

    };

    public static partial class LazyTranslations {
        
        static TranslationsEnum painterTranslations = new TranslationsEnum();

        public static LazyTranslation Get(this MsgPainter msg, int lang) {

            int index = (int) msg;

            if (painterTranslations.Initialized(index))
                return painterTranslations.GetWhenInited(index, lang);

            switch (msg) {
                case MsgPainter.PreserveTransparency:
                    msg.Translate("Preserve Transparency", "if every pixel of texture has alpha = 1 (Max) Unity will be save it as .png without transparency. To counter this " +
                                      " I set first pixels to alpha 0.9. I know it is hacky, it you know a better way, let me know")
                        .From(ukr, "Зберігати Альфа Канал", "Змінює прозорість першого пікселя на 0.9 щоб під час компресії алгоритм не забрав альфа канал.")
                        ;

                    break;
                case MsgPainter.BrushType:
                    msg.Translate("Brush Type")
                        .From(ukr, "Тип");
                    break;

                case MsgPainter.BlitMode:
                    msg.Translate("Blit Mode")
                        .From(ukr, "Метод");
                    break;

                case MsgPainter.LockToolToUseTransform:
                    msg.Translate("Lock texture to use transform tools. Or click 'Hide transform tool'")
                        .From(ukr, "Постав блок на текстурі щоб рухати обєкт, або натисни на 'Приховати трансформації' щоб не мішали.");
                    break;

                case MsgPainter.HideTransformTool:
                    msg.Translate("Hide transform tool").From(ukr, "Приховати трансформації");
                    break;
                case MsgPainter.AboutPlaytimePainter:
                    msg.Translate("About Playtime Painter Component",
                        ("This Component allows you to paint on this GameObject's Renderer's Material's Texture (Yes, there is a bit of hierarchy). It can also edit the mesh. " +
                         "All tools & configurations are accessible from within this inspector. " +
                         "Any changes are applied only to working copy of the texture and will be lost on Entering/Exiting Play mode or restarting Unity." +
                         "Load button on the bottom can reload working copy from original image file." +
                         "Save button will apply changes to the Original .png image. To save as new image, change name before saving and click Save As New." +
                         "Use Ctrl + Left Mouse Button to sample color of the texture." +
                         "Documentation is being integrated into the component (The blue '?' icons) .You can hide them from the Tool Settings. " +
                         "").F(pegi.EnvironmentNl))
                        .From(trk, "Playtime Painter Komponenti Hakkında" ,
                            "Bu komponent  sizin bu objenin işleyicisinin (renderer) materyalinin dokusunu ( material's texture) boyayabilmenizi sağlar ( Evet, burada biraz hiyerarşi var). Ayrıca meshi de düzenleyebilirsiniz.");
                    break;
                case MsgPainter.MeshProfileUsage:
                    msg.Translate("Mesh Profile usage", ("If using projected UV, place sharpNormal in TANGENT. {0}" +
                                                   "Vectors should be placed in normal and tangent slots to batch correctly.{0}" +
                                                   "Keep uv1 as is for baked light and damage shaders.{0}" +
                                                   "I place Shadows in UV2{0}" +
                                                   "I place Edge in UV3.{0}").F(pegi.EnvironmentNl));
                    break;
                case MsgPainter.Speed:
                    msg.Translate("Speed")
                        .From(ukr, "швидкість");
                    break;

                case MsgPainter.Scale:
                    msg.Translate("Scale")
                        .From(ukr, "розмір");
                    break;
                case MsgPainter.Hardness:
                    msg.Translate("Sharpness", "Makes edges more clear.")
                        .From(ukr, "різкість", "Робить краї кісточки більш чіткими.");
                    break;
                case MsgPainter.CopyFrom:
                    msg.Translate("Copy From")
                        .From(ukr, "Копіювати з");
                    break;
                case MsgPainter.TextureSettings:
                    msg.Translate("Texture settings")
                        .From(ukr, "Налаштування текстури");
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
                    msg.Translate("Opacity")
                        .From(ukr, "Непрозорість");
                    break;
                case MsgPainter.SpreadSpeed:
                    msg.Translate("Spread")
                        .From(ukr, "Поширення");
                    break;
                case MsgPainter.BlurAmount:
                    msg.Translate("Radius")
                        .From(ukr, "радіус");

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
                    msg.Translate(
                        "Projection ", 
                        "Will create a camera that will serve as a projector. This mode is similar to Copy, but instead of matching UV coordinates of source and target it will sample source " +
                        "using projector. Only World Space brushes can use this Blit Mode. Currently only sphere brush is a world space brush. First step is usually to position projector camera. "
                        );
                    break;

                case MsgPainter.BlitModeFiller:
                    msg.Translate(
                        "Ink Filler ", 
                        " Inspired by comic books. After you paint BLACK lines, this brush will try to gradually fill the painted area with color without crossing those lines. ");
                    break;
                case MsgPainter.Unnamed:
                    msg.Translate(
                        "Unnamed ",
                        "The selected class doesn't have a Readable name.");
                    break;
                #endregion
                case MsgPainter.TransparentLayer:
                    msg.Translate("Transparent",
                            "Toggle this ON if texture has transparent areas which will not be visible. This will affect how they are painted: color of the transparent areas will be neglected to avoid outline artifacts.")
                        .From(ukr, "Прозора текстура", "Під час малювання прозорих частин їхнім кольором нехтується. Це допомогає уникнути небажаних контурів.");
                    break;
                case MsgPainter.PleaseSelect:
                    msg.Translate("Painter tool is not selected (Select it in the top left area)")
                        .From(ukr, "Щоб редагувати текстуру або модель, оберіть Playtime Painter серед інструментів" +
                                   " в лівому верхньому куті екрану.");
                    break;


                case MsgPainter.MeshPoint:
                    msg.Translate("Mesh Point", "Mesh Point contain a number of vertices which share the same position");
                    break;
                case MsgPainter.Vertex:
                    msg.Translate("Vertex", "Each vertex can contain information like : position, UV sets, normal, tangent, color");
                    break;
                case MsgPainter.MeshPointPositionTool:
                    msg.Translate("Points Position", ("LMB - Drag {1} {0} " +
                                                      "Alt - Move {1} To Grid {0}" +
                                                      "U - make Triangle unique. {0}" +
                                                      "M - merge with nearest {1} while dragging {0}" +
                                                      "This tool also contains functionality related to smoothing and sharpening of the edges.")
                                                        .F(pegi.EnvironmentNl, MsgPainter.MeshPoint.GetText()));
                    break;

            }

            return painterTranslations.GetWhenInited(index, lang);
        }


#if PEGI

        private static int inspectingSection = -1;
        private static int inspectedFaqQuestion = -1;
        private static int inspectedPerfTip = -1;
        private static int inspectedUseful = -1;
        
        public static bool InspectPainterDocumentation()
        {
            var changed = false;

            if (inspectingSection == -1)
            MsgPainter.AboutPlaytimePainter.GetDescription().writeBig();

            if ("FAQ".enter(ref inspectingSection, 0).nl()) {

                if ("Can I integrate Painter into my game?".enter(ref inspectedFaqQuestion, 0).nl()) {

                    (" There is no reason why you can't. This asset doesn't contain or depend on any plugins and does everything using Unity functions. " +
                     " If you downloaded Examples folder, there should be simple scripts that use Paint functions." +
                     " Usually you will " +
                     " attach PlaytimePainter or some custom script to objects you want to be paintable. " +
                     " I try to make sure that all required information is provided trough the inspector" +
                     " interface. It should show warnings/hints when something needs to be set up. ").writeBig();
                }

                if ("How do I Save/Load/Undo changes to textures?".enter(ref inspectedFaqQuestion, 1).nl()) {
                    ("Undo/Redo needs to be enabled per texture in {0}. Otherwise there are just too many scenarios when lots of memory will be used due to unwanted backups and redo steps.".F(MsgPainter.TextureSettings.GetText()) +
                      " It is possible to save texture during runtime. In Texture Setting -> Texture Processors there is a section to Save/Load textures during runtime. It is there for testing." +
                      " The code used for saving texture is located inside ImgData class. While editing texture in editor, there is Save/Load buttons which can save changes to the actual .png file, or " +
                      "load from it. Of not pressed, any changes to the texture will be lost once Unity is restarted, or texture reimported."  ).writeBig();
                }

                if ("What should I be careful about?".enter(ref inspectedFaqQuestion, 2).nl())
                {
                    ("I tried to forsee as many scenarious as possible. So deleting something shouldn't be a problem for Playtime Painter." +
                     "It's best not to move the Tools folder or it's contents though. As of now the userbase is not huge, and feedback is scares. " +
                     "So I expect there to be issues, which can be easily fix, if reported. Clicking on SendEmail will open your email client with " +
                     " support email already typed in, so don't shy away from reporting those nasty bugs. See something, say something.").writeBig();
                }
             
            }

            if ("Performance/Quality tips".enter(ref inspectingSection, 1).nl())
            {
                if ("Slower in Editor in Android/iOS mode".enter(ref inspectedPerfTip, 0).nl())
                {
                    "If Editor is set for Android/iOS then Unity will try to emulate that API and as a result, painting will be slower."
                        .writeBig();
                }

                if ("Use GPU brush, big brush for CPU is slow.".enter(ref inspectedPerfTip, 1).nl()) {
                    "GPU brush is always faster for every task. CPU brush uses code written in C#, so it is much easier to add your own blit modes to it, but it gets slower the bigger the brush gets.".writeBig();
                }

                if ("Sphere brush is the best".enter(ref inspectedPerfTip, 2).nl()) {
                   ( " Sphere brush is the best for editing complex models. It uses world space instead of texture space so the result is often more as one would expect." +
                     " Since it is a GPU brush, performance should be good. It is possible to reduce the size of painting buffers in ToolSettings->PainterCamera->Buffers to increase performance." +
                     " Sphere brush doesn't always work well with tyling though. So when you see a line where brush seams to be able to paint only one side of it at any given time - tyling is the reason." +
                     " It is possible to slightly mitigate this issue by rendering multiple times. Option to do so may be added in future releases, but the option will" +
                     " undoubtfully come with performance cost (when used)"   ).writeBig();
                }

                if ("Fold In Material".enter(ref inspectedPerfTip, 3).nl())
                {
                    ("This one may be a temporary issue, but I noticed that when Inspector is showing a material, in some cases Editor slows down." +
                        "I often use the Lock icon on the Painter Component to hide other UI outside of component to maximize it's performance"
                            ).writeBig();
                }

            }

            if ("Good to know, release notes".enter(ref inspectingSection, 2).nl()) {

                if (inspectedUseful == -1) {
                    ("This section will most often relate to various findings related to Unity engine. I will also list all nuances and issues that may relate" +
                        " to current version only in most cases. ").writeBig();
                }

               // if ()

                if ("MSAA + HDR results in one frame delay".enter(ref inspectedUseful, 0).nl())
                {
                   // ("When I use Ray-Tracing camera I notice that it ")
                }

            }

            return changed;
        }



        public static void Write(this MsgPainter m) { var txt = m.GetText(); txt.write(txt.ApproximateLengthUnsafe()); }
        public static void Write(this MsgPainter m, int width) { m.GetText().write(width); }
        public static void Write(this MsgPainter m, string tip, int width) { m.GetText().write(tip, width); }
        public static void Write(this MsgPainter m, string tip) { var txt = m.GetText(); txt.write(tip, txt.ApproximateLengthUnsafe()); }
#endif
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

#if PEGI
        public static bool DocumentationClick(this MsgPainter msg) =>  PainterDataAndConfig.hideDocumentation ? false : msg.Get().DocumentationClick();



        public static bool DocumentationWarning(this MsgPainter msg) => PainterDataAndConfig.hideDocumentation ? false : msg.Get().WarningDocumentation();
#endif

        static LazyTranslation Get(this MsgPainter msg)
        {
            if (_systemLanguage == -1)
                Init();

            return msg.Get(_systemLanguage);
        }
        
        static Countless<LazyTranslation> Translate(this MsgPainter smg, string english) {
            var org = painterTranslations[(int)smg];
            org[eng] = new LazyTranslation(english);
            return org;
        }

        static Countless<LazyTranslation> Translate(this MsgPainter smg, string english, string englishDetails)
        {
            var org = painterTranslations[(int)smg];
            org[eng] = new LazyTranslation(english, englishDetails);
            return org;
        }


    }
}