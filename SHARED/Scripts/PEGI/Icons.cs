using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using System;
using SharedTools_Stuff;
// Icons management

namespace PlayerAndEditorGUI
{

    public enum icon
    {
        Alpha, Active,  Add, Animation, Audio,
        Back, Save, Close, Condition, Config, Copy, Create,
        Discord, Delete, Done, Docs, Download, Down, DownLast,
        Edit, Enter, Exit, Email, Empty,
        FoldedOut, Folder,
        NewMaterial, NewTexture, Next, On, Off, Lock, Unlock, GPU, CPU, Round,
        Square,  SaveAsNew, StateMachine, Show, PreviewShader, OriginalShader, Undo, Redo, Painter,
        UndoDisabled, RedoDisabled, Play, Record, Replace,  True, False,
        Script, Load, Pause, Mesh, Red, Green, Blue,  InActive, 
        Hint, Home, Hide,  Paste, Search, Refresh, Up, UpLast,  Warning, List, Link

    }

    public enum iconGackground
    {
        Frame

    }

    public static class Icons_MGMT
    {

        static List<Texture2D> backgrounds = new List<Texture2D>();

        static List<Texture2D> managementIcons;
        static List<Texture2D> painterIcons;

        static Texture ColorIcon(int ind)
        {
            if (painterIcons == null) painterIcons = new List<Texture2D>();

            while (painterIcons.Count <= ind) painterIcons.Add(null);

            if (painterIcons[ind] == null)
            {
                switch (ind)
                {
                    case 0: painterIcons[ind] = Resources.Load("icons/Red") as Texture2D; break;
                    case 1: painterIcons[ind] = Resources.Load("icons/Green") as Texture2D; break;
                    case 2: painterIcons[ind] = Resources.Load("icons/Blue") as Texture2D; break;
                    case 3: painterIcons[ind] = Resources.Load("icons/Alpha") as Texture2D; break;
                }
            }

            return (painterIcons[ind]);
        }

        public static Texture getIcon(this ColorChanel icon)
        {
            return ColorIcon((int)icon);
        }

        public static string ToText(this BrushMask icon)
        {
          
            switch (icon)
            {
                case BrushMask.R: return "Red";
                case BrushMask.G: return "Green";
                case BrushMask.B: return "Blue";
                case BrushMask.A: return "Alpha";
                default: return "Unknown channel";
            }


        }

        public static Texture GetIcon(this BrushMask icon)
        {
            int ind = 0;
            switch (icon)
            {
                case BrushMask.G: ind = 1; break;
                case BrushMask.B: ind = 2; break;
                case BrushMask.A: ind = 3; break;
            }

            return ColorIcon(ind);

        }

        public static Texture2D GetIcon(this icon icon)
        {

            if (managementIcons == null) managementIcons = new List<Texture2D>();

            int ind = (int)icon;
            while (managementIcons.Count <= ind) managementIcons.Add(null);

            if (managementIcons[ind] == null)
            {
                switch (icon)
                {
                    case icon.Red: return ColorIcon(0) as Texture2D;
                    case icon.Green: return ColorIcon(1) as Texture2D;
                    case icon.Blue: return ColorIcon(2) as Texture2D;
                    case icon.Alpha: return ColorIcon(3) as Texture2D;
                    default: return icon.Load();
                }
            }

            return (managementIcons[ind]);
        }

        public static Texture2D GetSprite(this iconGackground bg)
        {
            int ind = (int)bg;

            while (backgrounds.Count <= ind) backgrounds.Add(null);

            if (backgrounds[ind] == null) {
                string name = Enum.GetName(typeof(iconGackground), ind);

                if (bgloads > backgrounds.Count)
                    Debug.Log("Loading " + name);

                bgloads++;
                backgrounds[ind] = Resources.Load("bg/" + name) as Texture2D;
            }

            return backgrounds[ind];
        }

        static Texture2D LoadIcoRes(int ind, string name)
        {
            if (loads > managementIcons.Count)
                Debug.Log("Loading " + name);

            loads++;
            managementIcons[ind] = Resources.Load("icons/" + name) as Texture2D;
            return managementIcons[ind];
        }

        static Texture2D Load(this icon ico)
        {
            int ind = (int)ico;
            return LoadIcoRes(ind, Enum.GetName(typeof(icon), ind));
        }

        static int loads = 0;
        static int bgloads = 0;

    }
}