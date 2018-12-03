using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter
{

    public class MaterialData : PainterStuffKeepUnrecognized_STD, IPEGI, IPEGI_ListInspect, IGotDisplayName {
        
        #region Encode & Decode
        
        public Material material;
        public int _selectedTexture = 0;
        public bool usePreviewShader = false;
        public List<string> materials_TextureFields = new List<string>();

        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_Reference("mat", material)
            .Add_IfNotZero("texInd", _selectedTexture)
            .Add_IfTrue("pv", usePreviewShader)
            .Add("tf", materials_TextureFields);

        public override bool Decode(string tag, string data)
        {
           switch (tag)
            {
                case "mat": data.Decode_Reference(ref material); break;
                case "texInd":  _selectedTexture = data.ToInt(); break;
                case "pv": usePreviewShader = data.ToBool(); break;
                case "tf": data.Decode_List(out materials_TextureFields); break;
                default: return false;
            }
            return true;
        }
        #endregion

        [NonSerialized]
        public string bufferParameterTarget; // which texture is currently using RenderTexture buffer
        [NonSerialized]
        public PlaytimePainter painterTarget;

        public void SetTextureOnLastTarget(ImageData id) {
            if (painterTarget)
                painterTarget.SetTextureOnMaterial(bufferParameterTarget, id.CurrentTexture(), material);
        }

        public MaterialData (Material mat)  {
            material = mat;
        }

        public MaterialData()  {   }

        public string NameForPEGIdisplay => material == null ? "Error" : material.name;

        #region Inspector
        #if PEGI

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            this.ToPEGIstring().write_obj(90, material);
            if (icon.Enter.Click())
                edited = ind;
            material.clickHighlight();

            return false;
        }


        public override bool Inspect() {

            bool changed = false;

            "Material:".write_obj(60, material);
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
        #endregion
    }

   


}