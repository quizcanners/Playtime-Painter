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
        Add, Animation, Audio, Back, Save, Close, Condition, Config, Copy, Discord, Delete, Done, Docs,
        Edit, Enter, Exit, Email, FoldedOut,
        NewMaterial, NewTexture, Next, On, Off, Lock, Unlock, GPU, CPU, Round,
        Square, PreviewShader, OriginalShader, SaveAsNew, StateMachine, Undo, Redo, Painter,
        UndoDisabled, RedoDisabled, Play, Record,  
        Script, Load, Pause, Mesh, Red, Green, Blue, Alpha,
        Hint,  Paste, Search, Refresh, Up, Down

    }

    public static class Icons_MGMT
    {

        static List<Texture2D> managementIcons;
        static List<Texture2D> painterIcons;

        static Texture colorIcon(int ind)
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
            return colorIcon((int)icon);
        }



        public static Texture getIcon(this BrushMask icon)
        {
            int ind = 0;
            switch (icon)
            {
                case BrushMask.G: ind = 1; break;
                case BrushMask.B: ind = 2; break;
                case BrushMask.A: ind = 3; break;
            }

            return colorIcon(ind);

        }

        public static Texture2D getIcon(this icon icon)
        {

            if (managementIcons == null) managementIcons = new List<Texture2D>();

            int ind = (int)icon;
            while (managementIcons.Count <= ind) managementIcons.Add(null);

            if (managementIcons[ind] == null)
            {
                switch (icon)
                {
                    case icon.Red: return colorIcon(0) as Texture2D;
                    case icon.Green: return colorIcon(1) as Texture2D;
                    case icon.Blue: return colorIcon(2) as Texture2D;
                    case icon.Alpha: return colorIcon(3) as Texture2D;
                    default: return icon.load();
                }
            }

            return (managementIcons[ind]);
        }

        static Texture2D loadIcoRes(int ind, string name)
        {
            if (loads > managementIcons.Count)
                Debug.Log("Loading " + name);

            loads++;
            managementIcons[ind] = Resources.Load("icons/" + name) as Texture2D;
            return managementIcons[ind];
        }

        static Texture2D load(this icon ico)
        {
            int ind = (int)ico;
            return loadIcoRes(ind, Enum.GetName(typeof(icon), ind));
        }

        static int loads = 0;

    }
}