using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter
{

    public class MaterialMeta : PainterStuffKeepUnrecognized_STD, IPEGI, IPEGI_ListInspect, IGotDisplayName {
        
        public Material material;
        public int selectedTexture;
        public bool usePreviewShader;
        public List<ShaderProperty.TextureValue> materialsTextureFields = new List<ShaderProperty.TextureValue>();

        public ShaderProperty.TextureValue bufferParameterTarget; // which texture is currently using RenderTexture buffer
        public PlaytimePainter painterTarget;
        
        #region Encode & Decode
        
        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_Reference("mat", material)
            .Add_IfNotZero("texInd", selectedTexture)
            .Add_IfTrue("pv", usePreviewShader)
            .Add("tfs", materialsTextureFields);

        public override bool Decode(string tg, string data)
        {
           switch (tg)
            {
                case "mat": data.Decode_Reference(ref material); break;
                case "texInd":  selectedTexture = data.ToInt(); break;
                case "pv": usePreviewShader = data.ToBool(); break;
                case "tfs": data.Decode_List(out materialsTextureFields); break;
                default: return false;
            }
            return true;
        }
        #endregion

        public void SetTextureOnLastTarget(ImageMeta id) {
            if (painterTarget)
                painterTarget.SetTextureOnMaterial(bufferParameterTarget, id.CurrentTexture(), material);
        }

        public MaterialMeta (Material mat)  {
            material = mat;
        }

        public MaterialMeta()  {   }

        public string NameForDisplayPEGI => material == null ? "Error" : material.name;

        #region Inspector
        #if PEGI

        public bool PEGI_inList(IList list, int ind, ref int edited)
        {
            this.ToPEGIstring().write_obj(90, material);
            if (icon.Enter.Click())
                edited = ind;
            material.ClickHighlight();

            return false;
        }


        public override bool Inspect()
        {
            var changed = false;
            
            "Material:".write_obj(60, material);
            pegi.nl();

            if (material)
                ("Shader: " + material.shader).nl();

            "Textures".edit_List(ref materialsTextureFields).changes(ref changed);

            return false;
        }
        

#endif
        #endregion
    }

   


}