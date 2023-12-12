using QuizCanners.Inspect;
using UnityEngine;
using static QuizCanners.Utils.ShaderProperty;

namespace PainterTool
{
    public static class PainterShaderVariables
    {
        public const string GlobalPropertyPrefix = "g_";
        
  
        public const string ATLASED_TEXTURES = "_qcPp_AtlasTextures";

        public static readonly TextureValue PreviewTexture              = new("_qcPp_PreviewTex");

        public static readonly FloatValue TexturesInAtlasRow            = new(ATLASED_TEXTURES);
        public static readonly FloatValue BufferCopyAspectRatio         = new("_qcPp_BufferCopyAspectRatio");
        private static readonly VectorValue SourceTextureTexelSize       = new("_qcPp_BufferSourceTexelSize");

        public static Texture SourceTextureSize
        {
            set
            {
                if (value)
                    SourceTextureTexelSize.GlobalValue = new Vector4(1f/value.width, 1f/value.height, value.width, value.height);
            }
        }

        public static readonly VectorValue BRUSH_WORLD_POS_FROM         = new("_qcPp_brushWorldPosFrom");
        public static readonly VectorValue BRUSH_WORLD_POS_TO           = new("_qcPp_brushWorldPosTo");
        public static readonly VectorValue PREVIEW_BRUSH_UV_POS_FROM    = new("_qcPp_brushUvPosFrom");
        public static readonly VectorValue PREVIEW_BRUSH_UV_POS_TO      = new("_qcPp_brushUvPosTo");
        public static readonly VectorValue BRUSH_EDITED_UV_OFFSET       = new("_qcPp_brushEditedUVoffset");
        public static readonly VectorValue BRUSH_ATLAS_SECTION_AND_ROWS = new("_qcPp_brushAtlasSectionAndRows");
        public static readonly TextureValue DESTINATION_BUFFER          = new("_qcPp_DestBuffer");

        public static readonly FloatValue CopyColorTransparency         = new("_qcPp_CopyBlitAlpha");

        public static readonly ColorValue BrushColorProperty            = new("_qcPp_brushColor");

        public static readonly VectorValue ChannelCopySourceMask        = new("_qcPp_ChannelSourceMask");
        public static readonly VectorValue BrushMaskProperty            = new("_qcPp_brushMask");
        public static readonly VectorValue MaskDynamicsProperty         = new("_qcPp_maskDynamics");
        public static readonly VectorValue MaskOffsetProperty           = new("_qcPp_maskOffset");
        public static readonly VectorValue BrushFormProperty            = new("_qcPp_brushForm");
        public static readonly VectorValue TextureSourceParameters      = new("_qcPp_srcTextureUsage");
        public static readonly VectorValue cameraPosition_Property      = new("_qcPp_RTcamPosition");
        public static readonly VectorValue AlphaBufferConfigProperty    = new("_qcPp_AlphaBufferCfg");
        public static readonly VectorValue OriginalTextureTexelSize     = new("_qcPp_TargetTexture_TexelSize");

        public static readonly TextureValue SourceMaskProperty          = new("_qcPp_SourceMask");
        public static readonly TextureValue SourceTextureProperty       = new("_qcPp_SourceTexture");
        public static readonly TextureValue TransparentLayerUnderProperty = new("_qcPp_TransparentLayerUnderlay");
        public static readonly TextureValue AlphaPaintingBuffer         = new("_qcPp_AlphaBuffer");


        #region Shader Multicompile Keywords
        public const string UV_NORMAL = "_qcPp_UV_NORMAL";
        public const string UV_ATLASED = "_qcPp_UV_ATLASED";
        public const string UV_PROJECTED = "_qcPp_UV_PROJECTED";
        public const string UV_PIXELATED = "_qcPp_UV_PIXELATED";
        public const string EDGE_WIDTH_FROM_COL_A = "_qcPp_EDGE_WIDTH_FROM_COL_A";
       
        public const string BRUSH_TEXCOORD_2 = "_qcPp_BRUSH_TEXCOORD_2";
        public const string TARGET_TRANSPARENT_LAYER = "_qcPp_TARGET_TRANSPARENT_LAYER";
        public const string USE_DEPTH_FOR_PROJECTOR = "_qcPp_USE_DEPTH_FOR_PROJECTOR";

        public const string isAtlasedProperty = "_ATLASED";
        public const string isAtlasableDisaplyNameTag = "_ATL";
        public const string isUV2DisaplyNameTag = "_UV2";


        public const string MESH_PREVIEW_UV2 = "_qcPp_MESH_PREVIEW_UV2";
        public const string MESH_PREVIEW_LIT = "_qcPp_MESH_PREVIEW_LIT";
        public const string MESH_PREVIEW_NORMAL = "_qcPp_MESH_PREVIEW_NORMAL";
        public const string MESH_PREVIEW_VERTCOLOR = "_qcPp_MESH_PREVIEW_VERTCOLOR";
        public const string MESH_PREVIEW_PROJECTION = "_qcPp_MESH_PREVIEW_PROJECTION";
        #endregion

        public static void Inspect() 
        {
            "Painting to:".PegiLabel().Nl();
            DESTINATION_BUFFER.Inspect();

           

        }

    }
}