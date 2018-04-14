using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;

namespace Playtime_Painter
{
    [Serializable]
    public class MaterialData : iPEGI
    {
        public static int inspectedMaterial = -1;
        public static bool showMatDatas;

        public Material material;
        public bool lockEditing;
        public int _selectedTexture;
        public bool usePreviewShader = false;

        public List<string> materials_TextureFields = new List<string>();

        public MaterialData (Material mat)  {
            material = mat;
        }

        public bool PEGI() {
            bool changed = false;
            "Material:".write(60, material);
            pegi.nl();

            if (material)
                ("Shader: " + material.shader.ToString()).nl();

            if (materials_TextureFields.Count > 0) {
                "Textures: ".nl();
                foreach (var f in materials_TextureFields)
                    f.nl();
            }

            return changed;
        }
    }

   


}