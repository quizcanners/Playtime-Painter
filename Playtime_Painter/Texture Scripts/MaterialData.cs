using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter
{

    public class MaterialData : PainterStuffKeepUnrecognized_STD, IPEGI, IPEGI_ListInspect, IGotDisplayName
    {

        public static int inspectedMaterial = -1;
        public static bool showMatDatas;

        public Material material;
        public int _selectedTexture;
        public bool usePreviewShader = false;

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_Referance("mat", material)
            .Add("texInd", _selectedTexture)
            .Add_Bool("pv", usePreviewShader);

        public override bool Decode(string tag, string data)
        {
           switch (tag)
            {
                case "mat": data.Decode_Referance(ref material); break;
                case "texInd":  _selectedTexture = data.ToInt(); break;
                case "pv": usePreviewShader = data.ToBool(); break;
                default: return false;
            }
            return true;
        }


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

        public MaterialData()  {   }

        public string NameForPEGIdisplay() => material == null ? "Error" : material.name;


#if !NO_PEGI

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            this.ToPEGIstring().write(60, material);
            if (icon.Enter.Click())
                edited = ind;
            material.clickHighlight();

            return false;
        }


        public override bool PEGI() {
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