using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using PlayerAndEditorGUI;
using QuizCannersUtilities;


namespace Playtime_Painter{

    public static class TextureEditorExtensionFunctions  {

        public static void TeachingNotification(this string text)
        {
            if (PainterCamera.Data && PainterCamera.Data.showTeachingNotifications)
                text.showNotificationIn3D_Views();
        }

        public static float GetChanel(this Color col, ColorChanel chan)
        {
            switch (chan)
            {
                case ColorChanel.R:
                    return col.r;
                case ColorChanel.G:
                    return col.g;
                case ColorChanel.B:
                    return col.b;
                default:
                    return col.a;
            }
        }

        public static void SetChanel(this ColorChanel chan,  ref Color col,  float value)
        {
            switch (chan)
            {
                case ColorChanel.R:
                    col.r = value;
                    break;
                case ColorChanel.G:
                    col.g = value;
                    break;
                case ColorChanel.B:
                    col.b = value;
                    break;
                case ColorChanel.A:
                    col.a = value;
                    break;
            }
        }

        public static void Transfer(this BrushMask bm, ref Color col, Color c)
        {
            if ((bm & BrushMask.R) != 0)
                col.r = c.r;
            if ((bm & BrushMask.G) != 0)
                col.g =  c.g;
            if ((bm & BrushMask.B) != 0)
                col.b =  c.b;
            if ((bm & BrushMask.A) != 0)
                col.a =  c.a;
        }
    
        public static void Transfer(this BrushMask bm, ref Vector4 col, Color c)
        {
            if ((bm & BrushMask.R) != 0)
                col.x = c.r;
            if ((bm & BrushMask.G) != 0)
                col.y = c.g;
            if ((bm & BrushMask.B) != 0)
                col.z = c.b;
            if ((bm & BrushMask.A) != 0)
                col.w = c.a;
        }
    
        public static Mesh GetMesh(this PlaytimePainter p) {
            if (!p) return null;
    
            if (p.skinnedMeshRenderer)
                    return p.colliderForSkinnedMesh;
    
            return p.SharedMesh;
            
        }

        public static bool ContainsInstanceType(this List<PainterComponentPluginBase> collection, Type type){

		foreach (var t in collection) 
			if (t.GetType() == type) return true; 
		
		return false;
	}

		public static float StrokeWidth (this BrushConfig br, float pixWidth, bool world) => br.Size(world) / (pixWidth) * 2 * PainterCamera.OrthographicSize;
		
        public static bool IsSingleBufferBrush(this BrushConfig b) => (PainterCamera.Inst.isLinearColorSpace && b.GetBlitMode(false).SupportedBySingleBuffer && b.GetBrushType(false).SupportedBySingleBuffer && b.PaintingRGB);
        
        public static bool IsProjected(this Material mat) => mat && mat.shaderKeywords.Contains(PainterDataAndConfig.UV_PROJECTED);
        
        public static bool NeedsGrid (this PlaytimePainter painter) {
            if (!painter || !painter.enabled) return false;

            if (painter.meshEditing)
                return MeshManager.target == painter && PainterCamera.Data.MeshTool.ShowGrid;
            
            if (painter.LockTextureEditing || PainterCamera.Data.showConfig || !PlaytimePainter.IsCurrentTool)
                return false;

            return painter.GlobalBrushType.NeedsGrid; 
        }

        public static void AddIfNew<T>(this Dictionary<T, List<ImageMeta>> dic, T property, ImageMeta texture) where T:ShaderProperty.BaseShaderPropertyIndex
        {

            List<ImageMeta> mgmt;
            
            if (!dic.TryGetValue(property, out mgmt))
            {
                mgmt = new List<ImageMeta>();
                dic.Add(property, mgmt);
            }

            if (!mgmt.ContainsDuplicant(texture))
                mgmt.Add(texture);

        }
        
        public static bool TargetIsTexture2D(this ImageMeta id) =>  id != null && id.destination == TexTarget.Texture2D;
        
        public static bool TargetIsRenderTexture(this ImageMeta id) => id != null && id.destination == TexTarget.RenderTexture;

        public static ImageMeta GetImgDataIfExists(this Texture texture)
        {
            if (!texture || !PainterCamera.Data)
                return null;

            if (texture.IsBigRenderTexturePair() && PainterCamera.Inst.imgMetaUsingRendTex != null)
                return PainterCamera.Inst.imgMetaUsingRendTex;

            ImageMeta rid = null;

            var lst = PainterCamera.Data.imgMetas;

            if (lst == null) return rid;
            for (var i = 0; i < lst.Count; i++) {
                var id = lst[i];
                if ((texture != id.texture2D) && (texture != id.renderTexture) && (texture != id.other)) continue;
                
                rid = id;
                
                if (i > 3) 
                    PainterCamera.Data.imgMetas.Move(i, 0);
                
                break;
            }

            return rid;
        }
        
        public static ImageMeta GetImgData(this Texture texture)
        {
            if (!texture)
                return null;

            var nid = texture.GetImgDataIfExists() ?? new ImageMeta().Init(texture);

            return nid;
        }

        public static bool IsBigRenderTexturePair(this Texture tex) => tex && (tex ==  RenderTextureBuffersManager.GetPaintingBufferIfExist(0));

        private static bool ContainsDuplicant(this IList<ImageMeta> textures, ImageMeta other)
        {

            if (other == null)
                return true;

            for (var i = 0; i < textures.Count; i++)
                if (textures[i] == null) { textures.RemoveAt(i); i--; }

            return Enumerable.Contains(textures, other);
        }

        public static Texture GetDestinationTexture(this Texture texture)
        {

            var id = texture.GetImgDataIfExists();
            return id != null ? id.CurrentTexture() : texture;
        }

        public static RenderTexture CurrentRenderTexture(this ImageMeta id) => (id == null) ?  null :
            (id.renderTexture ? id.renderTexture : PainterCamera.FrontBuffer);
        
        public static Texture ExclusiveTexture(this ImageMeta id)
        {
            if (id == null)
                return null;

            if (id.other != null)
                return id.other;

            switch (id.destination)
            {
                case TexTarget.RenderTexture:
                    return !id.renderTexture ? (Texture)id.texture2D : (Texture)id.renderTexture;
                case TexTarget.Texture2D:
                    return id.texture2D;
            }
            return null;
        }

        public static Texture CurrentTexture(this ImageMeta id)
        {
            if (id == null)
                return null;

            if (id.other)
                return id.other;

            switch (id.destination)
            {
                case TexTarget.RenderTexture:
                    if (id.renderTexture != null)
                        return id.renderTexture;
                    if (PainterCamera.Inst.imgMetaUsingRendTex == id)
                        return PainterCamera.FrontBuffer;
                    id.destination = TexTarget.Texture2D;
                    return id.texture2D;
                case TexTarget.Texture2D:
                    return id.texture2D;
            }
            return null;
        }

        public static MaterialMeta GetMaterialPainterMeta (this Material mat) {
            return  PainterCamera.Data?.GetMaterialDataFor(mat);
        }
    }
}