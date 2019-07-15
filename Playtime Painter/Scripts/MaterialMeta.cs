using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace PlaytimePainter {

    public class MaterialMeta : PainterSystemKeepUnrecognizedCfg, IPEGI, IPEGI_ListInspect, IGotDisplayName {
        
        public Material material;
        public int selectedTexture;
        public bool usePreviewShader;
        public bool colorToVertexColorOnMerge;
        public bool selectedForMerge;
        public List<ShaderProperty.TextureValue> materialsTextureFields = new List<ShaderProperty.TextureValue>();

        public ShaderProperty.TextureValue bufferParameterTarget; // which texture is currently using RenderTexture buffer
        public PlaytimePainter painterTarget;
        
        #region Encode & Decode
        
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
        #endregion

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
            this.GetNameForInspector().writeUobj(90, material);
            if (icon.Enter.Click())
                edited = ind;
            material.ClickHighlight();

            return false;
        }
        
        public override bool Inspect() {

            var changed = false;
            
            "Material:".writeUobj(60, material);
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