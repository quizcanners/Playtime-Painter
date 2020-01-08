using static QuizCannersUtilities.ShaderProperty;

namespace PlaytimePainter
{
    public static class PainterShaderVariables
    {
        public const string GlobalPropertyPrefix = "g_";
        
        public const string TERRAIN_CONTROL_TEXTURE = "_qcPp_mergeControl";
        public const string TERRAIN_SPLAT_DIFFUSE = "_qcPp_mergeSplat_";
        public const string TERRAIN_NORMAL_MAP = "_qcPp_mergeSplatN_";
        public const string ATLASED_TEXTURES = "_qcPp_AtlasTextures";

        public static readonly VectorValue TerrainPosition              = new VectorValue("_qcPp_mergeTeraPosition");
        public static readonly VectorValue TerrainTiling                = new VectorValue("_qcPp_mergeTerrainTiling");
        public static readonly VectorValue TerrainScale                 = new VectorValue("_qcPp_mergeTerrainScale");
        public static readonly TextureValue TerrainHeight               = new TextureValue("_qcPp_mergeTerrainHeight");
        public static readonly TextureValue TerrainControlMain          = new TextureValue(TERRAIN_CONTROL_TEXTURE);

        public static readonly TextureValue TerrainLight                = new TextureValue("_qcPp_TerrainColors");
        public static readonly TextureValue PreviewTexture              = new TextureValue("_qcPp_PreviewTex");

        public static readonly FloatValue TexturesInAtlasRow            = new FloatValue(ATLASED_TEXTURES);
        public static readonly FloatValue BufferCopyAspectRatio         = new FloatValue("_qcPp_BufferCopyAspectRatio");

        public static readonly VectorValue BRUSH_WORLD_POS_FROM         = new VectorValue("_qcPp_brushWorldPosFrom");
        public static readonly VectorValue BRUSH_WORLD_POS_TO           = new VectorValue("_qcPp_brushWorldPosTo");
        public static readonly VectorValue BRUSH_POINTED_UV             = new VectorValue("_qcPp_brushPointedUV");
        public static readonly VectorValue BRUSH_EDITED_UV_OFFSET       = new VectorValue("_qcPp_brushEditedUVoffset");
        public static readonly VectorValue BRUSH_ATLAS_SECTION_AND_ROWS = new VectorValue("_qcPp_brushAtlasSectionAndRows");
        public static readonly TextureValue DESTINATION_BUFFER          = new TextureValue("_qcPp_DestBuffer");

        public static readonly FloatValue CopyColorTransparency         = new FloatValue("_qcPp_CopyBlitAlpha");

        public static readonly VectorValue BrushColorProperty           = new VectorValue("_qcPp_brushColor");

        public static readonly VectorValue ChannelCopySourceMask        = new VectorValue("_qcPp_ChannelSourceMask");
        public static readonly VectorValue BrushMaskProperty            = new VectorValue("_qcPp_brushMask");
        public static readonly VectorValue MaskDynamicsProperty         = new VectorValue("_qcPp_maskDynamics");
        public static readonly VectorValue MaskOffsetProperty           = new VectorValue("_qcPp_maskOffset");
        public static readonly VectorValue BrushFormProperty            = new VectorValue("_qcPp_brushForm");
        public static readonly VectorValue TextureSourceParameters      = new VectorValue("_qcPp_srcTextureUsage");
        public static readonly VectorValue cameraPosition_Property      = new VectorValue("_qcPp_RTcamPosition");
        public static readonly VectorValue AlphaBufferConfigProperty    = new VectorValue("_qcPp_AlphaBufferCfg");
        public static readonly VectorValue OriginalTextureTexelSize     = new VectorValue("_qcPp_TargetTexture_TexelSize");

        public static readonly TextureValue SourceMaskProperty          = new TextureValue("_qcPp_SourceMask");
        public static readonly TextureValue SourceTextureProperty       = new TextureValue("_qcPp_SourceTexture");
        public static readonly TextureValue TransparentLayerUnderProperty = new TextureValue("_qcPp_TransparentLayerUnderlay");
        public static readonly TextureValue AlphaPaintingBuffer         = new TextureValue("_qcPp_AlphaBuffer");


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
    }
}