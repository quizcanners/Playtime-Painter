using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
// Icons management

public enum icon {
    save, NewMaterial, NewTexture, On, Off, Lock, Unlock, GPU, CPU, Round,
    Square, PreviewShader, OriginalShader, saveAsNew, Undo, Redo, Painter,
    UndoDisabled, RedoDisabled, Play, Record, Delete, Done, Edit, Close, Add,
    Script, Config, Load, Pause, mesh
}

public static class Icons_MGMT {
    
    static List<Texture2D> managementIcons;
    static List<Texture2D> painterIcons;


  

    public static Texture getIcon(this BrushMask icon) {

        if (painterIcons == null) painterIcons = new List<Texture2D>();

        int ind = (int)icon;
        while (painterIcons.Count <= ind) painterIcons.Add(null);

        if (painterIcons[ind] == null) {
            switch (icon) {
                case BrushMask.R: painterIcons[ind] = Resources.Load("icons/Red") as Texture2D; break;
                case BrushMask.G: painterIcons[ind] = Resources.Load("icons/Green") as Texture2D; break;
                case BrushMask.B: painterIcons[ind] = Resources.Load("icons/Blue") as Texture2D; break;
                case BrushMask.A: painterIcons[ind] = Resources.Load("icons/Alpha") as Texture2D; break;
            }
        }

        return (painterIcons[ind]);
    }

    public static Texture2D getIcon(this icon icon) {

        if (managementIcons == null) managementIcons = new List<Texture2D>();

        int ind = (int)icon;
        while (managementIcons.Count <= ind) managementIcons.Add(null);

        if (managementIcons[ind] == null) {
            switch (icon) {
                case icon.save: managementIcons[ind] = Resources.Load("icons/save") as Texture2D; break;
                case icon.NewMaterial: managementIcons[ind] = Resources.Load("icons/NewMaterial") as Texture2D; break;
                case icon.NewTexture: managementIcons[ind] = Resources.Load("icons/NewTexture") as Texture2D; break;
                case icon.Off: managementIcons[ind] = Resources.Load("icons/Off") as Texture2D; break;
                case icon.On: managementIcons[ind] = Resources.Load("icons/On") as Texture2D; break;
                case icon.Lock: managementIcons[ind] = Resources.Load("icons/Lock") as Texture2D; break;
                case icon.Unlock: managementIcons[ind] = Resources.Load("icons/Unlock") as Texture2D; break;
                case icon.GPU: managementIcons[ind] = Resources.Load("icons/GPU") as Texture2D; break;
                case icon.CPU: managementIcons[ind] = Resources.Load("icons/CPU") as Texture2D; break;
                case icon.Round: managementIcons[ind] = Resources.Load("icons/Round") as Texture2D; break;
                case icon.Square: managementIcons[ind] = Resources.Load("icons/Square") as Texture2D; break;
                case icon.PreviewShader: managementIcons[ind] = Resources.Load("icons/PreviewShader") as Texture2D; break;
                case icon.OriginalShader: managementIcons[ind] = Resources.Load("icons/OriginalShader") as Texture2D; break;
                case icon.saveAsNew: managementIcons[ind] = Resources.Load("icons/saveAsNew") as Texture2D; break;
                case icon.Undo: managementIcons[ind] = Resources.Load("icons/Undo") as Texture2D; break;
                case icon.Redo: managementIcons[ind] = Resources.Load("icons/Redo") as Texture2D; break;
                case icon.Painter: managementIcons[ind] = Resources.Load("icons/Painter") as Texture2D; break;
                case icon.UndoDisabled: managementIcons[ind] = Resources.Load("icons/UndoDisabled") as Texture2D; break;
                case icon.RedoDisabled: managementIcons[ind] = Resources.Load("icons/RedoDisabled") as Texture2D; break;
                case icon.Play: managementIcons[ind] = Resources.Load("icons/Play") as Texture2D; break;
                case icon.Record: managementIcons[ind] = Resources.Load("icons/Record") as Texture2D; break;
                case icon.Delete: managementIcons[ind] = Resources.Load("icons/Delete") as Texture2D; break;
                case icon.Done: managementIcons[ind] = Resources.Load("icons/Done") as Texture2D; break;
                case icon.Edit: managementIcons[ind] = Resources.Load("icons/Edit") as Texture2D; break;
                case icon.Close: managementIcons[ind] = Resources.Load("icons/Close") as Texture2D; break;
                case icon.Add: managementIcons[ind] = Resources.Load("icons/Add") as Texture2D; break;
                case icon.Script: managementIcons[ind] = Resources.Load("icons/Script") as Texture2D; break;
                case icon.Config: managementIcons[ind] = Resources.Load("icons/Config") as Texture2D; break;
                case icon.Load: managementIcons[ind] = Resources.Load("icons/Load") as Texture2D; break;
                case icon.Pause: managementIcons[ind] = Resources.Load("icons/Pause") as Texture2D; break;
                case icon.mesh: managementIcons[ind] = Resources.Load("icons/mesh") as Texture2D; break;
            }
        }

        return (managementIcons[ind]);
    }

   

}