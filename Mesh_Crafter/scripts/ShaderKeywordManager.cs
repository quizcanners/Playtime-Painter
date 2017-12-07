using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class meshSHaderMode {
    // Construction
    private static List<meshSHaderMode> _allModes = new List<meshSHaderMode>();

    public static List<meshSHaderMode> allModes { get { return _allModes; } }

    private meshSHaderMode(string value) { Value = value; _allModes.Add(this); }

    public string Value;

    public static meshSHaderMode lit = new meshSHaderMode("MESH_PREVIEW_LIT");
    public static meshSHaderMode normVector = new meshSHaderMode("MESH_PREVIEW_NORMAL");
    public static meshSHaderMode vertColor = new meshSHaderMode("MESH_PREVIEW_VERTCOLOR");
    public static meshSHaderMode projection = new meshSHaderMode("MESH_PREVIEW_PROJECTION");
    public static meshSHaderMode smoothNormal = new meshSHaderMode("MESH_PREVIEW_SHARP_NORMAL");

    // Functionality
    private static meshSHaderMode selected;

    public override string ToString() {
        return Value;
    }

    public bool isSelected { get { return selected == this; } }

    public static void ApplySelected() {
        if (selected == null)
            selected = _allModes[0];
        selected.Apply();
    }

    public void Apply() {
        selected = this;

        foreach (meshSHaderMode s in _allModes)
            if (this == s)
                Shader.EnableKeyword(s.Value);
            else
                Shader.DisableKeyword(s.Value);
    }

}



