using System;
using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;

namespace PlaytimePainter {

    [Serializable]
    public class MaterialMeta : PainterClass, IPEGI, IPEGI_ListInspect, IGotDisplayName {
        
        [SerializeField] public Material material;
        [SerializeField] public int selectedTexture;
        [SerializeField] public bool usePreviewShader;
        [SerializeField] public bool colorToVertexColorOnMerge;
        [SerializeField] public bool selectedForMerge;
        [SerializeField] public List<ShaderProperty.TextureValue> materialsTextureFields = new List<ShaderProperty.TextureValue>();

        [NonSerialized] public ShaderProperty.TextureValue bufferParameterTarget; // which texture is currently using RenderTexture buffer
        [NonSerialized] public PlaytimePainter painterTarget;
        
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

        public string NameForDisplayPEGI()=> material == null ? "Error" : material.name;

        #region Inspector

        public bool InspectInList(IList list, int ind, ref int edited)
        {
            pegi.write(material);
            if (icon.Enter.Click())
                edited = ind;

            return false;
        }
        
        public bool Inspect() {

            var changed = false;
            
            "Material:".write(60, material);
            pegi.nl();

            if (material) {

                ("Shader: " + material.shader).write();

                if (material.shader)
                    material.shader.ClickHighlight().nl(ref changed);
            }

            "Color to Vertex color on Merge".toggleIcon(ref colorToVertexColorOnMerge).nl(ref changed);

            "Textures".edit_List(ref materialsTextureFields).changes(ref changed);

            if (material) {

                var colorFields = material.GetColorProperties();

                if (colorFields.Count > 0) {

                    "Colors".nl(PEGI_Styles.ListLabel);

                    foreach (var colorField in colorFields)
                    {
                        colorField.write_ForCopy();
                        pegi.nl();
                    }

                    //"Colors".write_List(colorFields).nl();
                }
            }

            return false;
        }
        
        #endregion
    }

   


}