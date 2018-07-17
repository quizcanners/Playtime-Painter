using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;

namespace Playtime_Painter
{
    [Serializable]
    public class MaterialData : IPEGI
    {

      //  public static MaterialData lastFetched;
      //  public static Material lastFetchedFor;

        public static int inspectedMaterial = -1;
        public static bool showMatDatas;

        public Material material;
        public int _selectedTexture;
        public bool usePreviewShader = false;

        [NonSerialized]
        public string bufferParameterTarget; // which texture is currently using RenderTexture buffer
        [NonSerialized]
        public PlaytimePainter painterTarget;

        public void SetTextureOnLastTarget(ImageData id) {
            if (painterTarget)
                painterTarget.SetTextureOnMaterial(bufferParameterTarget, id.CurrentTexture(), material);
        }

        public List<string> materials_TextureFields = new List<string>();

        public MaterialData (Material mat)  {
            material = mat;
        }

        public override string ToString()
        {
            return material == null ? "Error" : material.name;
        }

        public MaterialData()
        {
          //  Debug.Log("No material parameter assigned to data");
        }

        #if PEGI
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
#endif
    }

   


}