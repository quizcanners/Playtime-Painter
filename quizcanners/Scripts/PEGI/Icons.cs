using System.Collections.Generic;
using UnityEngine;
using System;
using QuizCannersUtilities;

namespace PlayerAndEditorGUI
{

    public enum icon
    {
        Alpha, Active,  Add, Animation, Audio,
        Back, Book, Save, Close, Condition, Config, Copy, Cut, Create, Clear,
        Discord, Delete, Done, Docs, Download, Down, DownLast, Debug, DeSelectAll,
        Edit, Enter, Exit, Email, Empty,
        False, FoldedOut, Folder,
        NewMaterial, NewTexture, Next, On, Off,
        Lock, Unlock, List, Link, UnLinked,
        GPU, CPU, Round,
        Search, Script, Square,  SaveAsNew, StateMachine, State, Show, SelectAll, Share, Question,
        Painter, PreviewShader, OriginalShader, Undo, Redo, 
        UndoDisabled, RedoDisabled, Play, Record, Replace,  True, 
        Load, Pause, Mesh, Move, Red, Green, Blue,  InActive, 
        Hint, Home, Hide,  Paste,  Refresh, Up, UpLast,  Warning, Wait
      


    }

    public static class Icons_MGMT
    {

        private static List<Texture2D> _managementIcons;
        
        public static Texture2D GetIcon(this icon icon)
        {

            if (_managementIcons == null) _managementIcons = new List<Texture2D>();

            var ind = (int)icon;
            while (_managementIcons.Count <= ind) _managementIcons.Add(null);

            if (_managementIcons[ind] != null) return (_managementIcons[ind]);
            
            switch (icon)
            {
                case icon.Red: return ColorIcon(0) as Texture2D;
                case icon.Green: return ColorIcon(1) as Texture2D;
                case icon.Blue: return ColorIcon(2) as Texture2D;
                case icon.Alpha: return ColorIcon(3) as Texture2D;
                default: return icon.Load();
            }

        }


        private static Texture2D LoadIcoRes(int ind, string name)
        {
            if (_loads > _managementIcons.Count)
                Debug.Log("Loading " + name);

            _loads++;
            _managementIcons[ind] = Resources.Load("icons/" + name) as Texture2D;
            return _managementIcons[ind];
        }

        private static Texture2D Load(this icon ico)
        {
            var ind = (int)ico;
            return LoadIcoRes(ind, Enum.GetName(typeof(icon), ind));
        }

        private static int _loads;
        private static int _bgLoads;
       
        #region Color Icons
        private static List<Texture2D> _painterIcons;

        private static Texture ColorIcon(int ind)
        {
            if (_painterIcons == null) _painterIcons = new List<Texture2D>();

            while (_painterIcons.Count <= ind) _painterIcons.Add(null);

            if (_painterIcons[ind] != null) return (_painterIcons[ind]);
            switch (ind)
            {
                case 0:
                    _painterIcons[ind] = Resources.Load("icons/Red") as Texture2D;
                    break;
                case 1:
                    _painterIcons[ind] = Resources.Load("icons/Green") as Texture2D;
                    break;
                case 2:
                    _painterIcons[ind] = Resources.Load("icons/Blue") as Texture2D;
                    break;
                case 3:
                    _painterIcons[ind] = Resources.Load("icons/Alpha") as Texture2D;
                    break;
            }

            return (_painterIcons[ind]);
        }

        public static Texture GetIcon(this ColorChanel icon) => ColorIcon((int)icon);
        

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
            var ind = 0;
            switch (icon)
            {
                case BrushMask.G: ind = 1; break;
                case BrushMask.B: ind = 2; break;
                case BrushMask.A: ind = 3; break;
            }

            return ColorIcon(ind);

        }
        #endregion

    }
}