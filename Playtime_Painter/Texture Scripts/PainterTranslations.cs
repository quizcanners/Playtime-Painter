using System;
using System.Runtime.CompilerServices;
using Playtime_Painter;
using QuizCannersUtilities;

namespace PlayerAndEditorGUI {

    public enum MsgPainter {
        PreserveTransparency, BrushType, BlitMode, LockToolToUseTransform, HideTransformTool, AboutPlaytimePainter,
         MeshProfileUsage, Speed, Scale, Hardness, CopyFrom, FancyOptions, previewRGBA, AutoSelectMaterial,
         aboutDisableDocumentation, SampleColor, PreviewRecommended, AlphaBufferBlit, Opacity, SpreadSpeed, BlurAmount
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
                        .From(ukr, "Памятати прозорість", "Змінює прозорість першого пікселя на 0.9 щоб під час компресії алгоритм не забрав альфа канал.")
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
                        ("This Component allows you to paint on this object's renderer's material's texture (Yes, there is a bit of hierarchy). It can also edit the mesh. " +
                         "All functions & configurations are accessible from within this inspector. " +
                         "Any changes are applied only to working copy of the texture and will be lost on Entering/Exiting Play mode or restarting Unity." +
                         "Load button on the bottom can reload working copy from original image file." +
                         "Save button will apply changes to the original file. To save as new file, change name before saving and click Save As New." +
                         "Use Ctrl + Left Mouse Button to sample color from texture." +
                         "I tried to integrate tutorial into the component (Click on blue '?') .You can hide them from the configuration. " +
                         "").F(pegi.EnvironmentNl));
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
                    msg.Translate("Sharpness")
                        .From(ukr, "різкість");
                    break;
                case MsgPainter.CopyFrom:
                    msg.Translate("Copy From")
                        .From(ukr, "Копіювати з");
                    break;
                case MsgPainter.FancyOptions:
                    msg.Translate("Fancy options")
                        .From(ukr, "Налаштування");
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
            }

            return painterTranslations.GetWhenInited(index, lang);
        }

        public static void Write(this MsgPainter m) { var txt = m.GetText(); txt.write(txt.ApproximateLengthUnsafe()); }
        public static void Write(this MsgPainter m, int width) { m.GetText().write(width); }
        public static void Write(this MsgPainter m, string tip, int width) { m.GetText().write(tip, width); }

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

        public static bool Documentation(this MsgPainter msg) =>  PainterDataAndConfig.hideDocumentation ? false : msg.Get().Documentation();

        public static bool DocumentationWarning(this MsgPainter msg) => PainterDataAndConfig.hideDocumentation ? false : msg.Get().WarningDocumentation();


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