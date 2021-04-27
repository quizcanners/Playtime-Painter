using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool {

    [Serializable]
    internal class MaterialMeta : PainterClass, IPEGI, IPEGI_ListInspect
    {    
        [SerializeField] public Material material;
        [SerializeField] public int selectedTexture;
        [SerializeField] public bool usePreviewShader;
        [SerializeField] public bool colorToVertexColorOnMerge;
        [SerializeField] public bool selectedForMerge;
        [SerializeField] public List<ShaderProperty.TextureValue> materialsTextureFields = new List<ShaderProperty.TextureValue>();

        [NonSerialized] public ShaderProperty.TextureValue bufferParameterTarget; // which texture is currently using RenderTexture buffer
        [NonSerialized] public PainterComponent painterTarget;
        
      /*  #region Encode & Decode
        
        public override CfgEncoder Encode() => this.EncodeUnrecognized()
            .Add_Reference("mat", material)
            .Add_IfNotZero("texInd", selectedTexture)
            .Add_IfTrue("pv", usePreviewShader)
            .Add_IfTrue("colToV", colorToVertexColorOnMerge)
            .Add("tfs", materialsTextureFields);

        public override bool Decode(string tg, string data)
        {
           switch (tg)
            {
                case "mat": data.Decode_Reference(ref material); break;
                case "texInd":  selectedTexture = data.ToInt(); break;
                case "pv": usePreviewShader = data.ToBool(); break;
                case "tfs": data.Decode_List(out materialsTextureFields); break;
                case "colToV": colorToVertexColorOnMerge = data.ToBool(); break;
                default: return false;
            }
            return true;
        }
        #endregion*/

        public void SetTextureOnLastTarget(TextureMeta id) {
            if (painterTarget)
                painterTarget.SetTextureOnMaterial(bufferParameterTarget, id.CurrentTexture(), material);
        }

        public MaterialMeta (Material mat)  {
            material = mat;
        }

        public MaterialMeta()  {   }

        public override string ToString() => material == null ? "Error" : material.name;

        #region Inspector

        public void InspectInList(ref int edited, int ind)
        {
            pegi.Write(material);
            if (Icon.Enter.Click())
                edited = ind;
        }
        
        public void Inspect() {

            "Material:".PegiLabel(60).Write(material);
            pegi.Nl();

            if (material) {

                ("Shader: " + material.shader).PegiLabel().Write();

                if (material.shader)
                    pegi.ClickHighlight(material.shader).Nl();
            }

            "Color to Vertex color on Merge".PegiLabel().ToggleIcon(ref colorToVertexColorOnMerge).Nl();

            "Textures".PegiLabel().Edit_List(materialsTextureFields);

            if (material) {

                var colorFields = material.GetColorProperties();

                if (colorFields.Count > 0) {

                    "Colors".PegiLabel(style: pegi.Styles.ListLabel).Nl();

                    foreach (var colorField in colorFields)
                    {
                        colorField.PegiLabel().Write_ForCopy();
                        pegi.Nl();
                    }

                    //"Colors".write_List(colorFields).nl();
                }
            }
        }
        
        #endregion
    }

   


}