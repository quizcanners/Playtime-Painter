using QuizCanners.Inspect;
using QuizCanners.Utils;
using System;
using UnityEngine;

namespace PainterTool
{
    public partial class PainterComponent 
    {

        internal static class Documentation 
        {
            private static readonly pegi.EnterExitContext _context = new pegi.EnterExitContext();

            private static readonly pegi.EnterExitContext _context_HowToPaint = new pegi.EnterExitContext();
            private static readonly pegi.EnterExitContext _context_FAQ = new pegi.EnterExitContext();
            private static readonly pegi.EnterExitContext _context_PerfTip = new pegi.EnterExitContext();
            private static readonly pegi.EnterExitContext _context_Useful = new pegi.EnterExitContext();

            public static void Inspect()
            {
                using (_context.StartContext())
                {
                    if ("About".PL().IsEntered().Nl())
                    {
                        ("This Component allows you to paint on this GameObject's Renderer's Material's Texture (Yes, there is a bit of hierarchy). It can also edit the mesh. " +
                        "All tools & configurations are accessible from within this inspector. " +
                        "Painting is applied to the texture in RAM. When Entering/Exiting Play mode or restarting Unity changes to texture will be reverted." +
                        "Load button on the bottom can reload pixels from original image file (Same as opening/closng Unity)." +
                        "Save button will apply changes to the original .png image. To save as new image, change name before saving and click Save As New. {0}" +
                        "Sometimes Painter's UI offers to change import settings, they will persist. " +
                        "Use Ctrl + Left Mouse Button to sample color of the texture." +
                        "Documentation is being integrated into the component (The blue '?' icons) .You can hide them from the Tool Settings. " +
                        "").F(pegi.EnvironmentNl).PL().WriteBig();
                    }

                    if ("How to Use Painter".PL().IsEntered().Nl())
                    {
                        using (_context_HowToPaint.StartContext())
                        {
                            if ("Painter Component".PL().IsEntered().Nl())
                            {
                                "{0} provides the default interface to all of the Platime Painter functionality.".F(nameof(PainterComponent)).PL().WriteBig();
                                ("To modify textures during gameplay you should use {0} class directly. Create a command by feeding it " +
                                    "Texture (what to paint), Stroke(where to paint) and Brush(how to paint it)").F(nameof(Painter.Command)).PL().WriteBig();
                            }

                            if ("Painter Camera".PL().IsEntered().Nl())
                            {
                                "{0} will be automatically created in the scene to help manage all the painting processes.".F(nameof(Singleton_PainterCamera)).PL().WriteBig();
                            }

                            if ("Paint Command: (Texture + Brush + Stroke)".PL().IsEntered().Nl())
                            {
                                "{0} usually requires 3 elements to be created. It can also be reused. For World-Space brush you will also need to provide the Game Object.".F(nameof(Painter.Command)).PL().WriteBig();
                            }

                            if ("Brush Configuration".PL().IsEntered().Nl())
                            {
                                ("{0} class contains all the configurations for a brush. While all instances of Painter Tool use a shared static instance. " +
                                    "For your other needs you can create more brushes. Examples included with the asset use {1} to configure additional brushes.")
                                    .F(nameof(Brush), nameof(SO_BrushConfigScriptableObject)).PL().WriteBig();
                            }

                            if ("Stroke".PL().IsEntered().Nl())
                            {
                                ("{0} is class that contains coordinates of where to paint. It would be World Space coordinates for Sphere Brush, and UV-space coordinates for other brushes." +
                                    "Stroke will often be created from RaycastHit result. " +
                                    "In case you want continious painting, preserve the instance of the stroke to remember previous position.")
                                    .F(nameof(Stroke)).PL().WriteBig();
                            }
                        }
                    }

                    if ("FAQ".PL().IsEntered().Nl())
                    {
                        using (_context_FAQ.StartContext())
                        {
                            if ("Can I integrate Painter into my game?".PL().IsEntered().Nl())
                            {
                                (" There is no reason why you can't. This asset doesn't contain or depend on any plugins and does everything using Unity functions. " + Environment.NewLine +
                                 " If you have downloaded Examples folder, there should be a simple scripts that use Paint functions." + Environment.NewLine +
                                 " Usually you will attach PlaytimePainter or some custom script to objects you want to paint on. " + Environment.NewLine +
                                 " I try to make sure that all required information is provided trough the inspector" +
                                 " interface. It should show warnings/hints when something needs to be set up. ").PL().WriteBig();
                            }

                            if ("How do I Save/Load/Undo changes to textures?".PL().IsEntered().Nl())
                            {
                                ("Undo/Redo needs to be enabled per texture in {0}. Otherwise there are just too many scenarios when lots of memory will be used due to unwanted backups and redo steps.".F(MsgPainter.TextureSettings.GetText()) +
                                  " It is possible to save texture during runtime. In Texture Setting -> Texture Processors there is a section to Save/Load textures during runtime. It is there for testing." +
                                  " The code used for saving texture is located inside ImgData class. While editing texture in editor, there is Save/Load buttons which can save changes to the actual .png file, or " +
                                  "load from it. If not saved, any changes to the texture will be lost once Unity is restarted, or texture reimported.").PL().WriteBig();
                            }

                            if ("What should I be careful about?".PL().IsEntered().Nl())
                            {
                                ("I tried to forsee as many scenarious as possible. So deleting something shouldn't be a problem for Playtime Painter." +
                                 "It's best not to move the Tools folder or it's contents though. As of now the userbase is not huge, and feedback is scares. " +
                                 "So I expect there to be issues, which can be easily fix, if reported. Clicking on SendEmail will open your email client with " +
                                 " support email already typed in, so don't shy away from reporting those nasty bugs. See something, say something.").PL().WriteBig();
                            }
                        }
                    }

                    if ("Performance/Quality tips".PL().IsEntered().Nl())
                    {
                        using (_context_PerfTip.StartContext())
                        {
                            if ("Slower in Editor in Android/iOS mode".PL().IsEntered().Nl())
                            {
                                "If Editor is set for Android/iOS then Unity will try to emulate that API and as a result, painting will be slower.".PL()
                                    .WriteBig();
                            }

                            if ("Use GPU brush, big brush for CPU is slow.".PL().IsEntered().Nl())
                            {
                                "GPU brush is always faster for every task. CPU brush uses code written in C#, so it is much easier to add your own blit modes to it, but it gets slower the bigger the brush gets.".PL().WriteBig();
                            }

                            if ("Sphere brush is the best".PL().IsEntered().Nl())
                            {
                                (" Sphere brush is the best for editing complex models. It uses world space instead of texture space so the result is often more as one would expect." +
                                  " Since it is a GPU brush, performance should be good. It is possible to reduce the size of painting buffers in ToolSettings->PainterCamera->Buffers to increase performance." +
                                  " Sphere brush doesn't always work well with tyling though. So when you see a line where brush seams to be able to paint only one side of it at any given time - tyling is the reason." +
                                  " It is possible to slightly mitigate this issue by rendering multiple times. Option to do so may be added in future releases, but the option will" +
                                  " undoubtfully come with performance cost (when used)").PL().WriteBig();
                            }

                            if ("Fold In Material".PL().IsEntered().Nl())
                            {
                                ("This one may be a temporary issue, but I noticed that when Inspector is showing a material, in some cases Editor slows down." +
                                    "I often use the Lock icon on the Painter Component to hide other UI outside of component to maximize it's performance"
                                        ).PL().WriteBig();
                            }
                        }
                    }

                    if ("Good to know, release notes".PL().IsEntered().Nl())
                    {
                        using (_context_Useful.StartContext())
                        {
                            if (_context_Useful.IsAnyEntered == false)
                            {
                                ("This section will most often relate to various findings related to Unity engine. I will also list all nuances and issues that may relate" +
                                    " to current version only in most cases. ").PL().WriteBig();
                            }

                            if ("MSAA + HDR results in one frame delay".PL().IsEntered().Nl())
                            {

                            }
                        }
                    }

                    if ("Useful Links".PL().IsEntered().Nl())
                    {
                        " Something I often use when writing shaders. ".PL().WriteBig();

                        if ("Simple circle shader template".PL().ClickText(15).Nl())
                            Application.OpenURL("https://gist.github.com/quizcanners/b90d2644d7b990d0574307218478383a");

                        if ("Shader Cheat-Sheet".PL().ClickText(15).Nl())
                            Application.OpenURL("https://gist.github.com/quizcanners/0da1cbad4b1e2187af73f6ab52a6dabb");

                        if ("Shader commands".PL().ClickText(15).Nl())
                            Application.OpenURL("https://gist.github.com/quizcanners/6bc0b06172977c1a324e81e626079fb2");
                    }
                }


            }
        }



    }
}