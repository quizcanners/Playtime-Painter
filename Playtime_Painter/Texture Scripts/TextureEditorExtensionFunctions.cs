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
using SharedTools_Stuff;


namespace Playtime_Painter{

public static class TextureEditorExtensionFunctions  {
        
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
        if (p == null) return null;
        if (p.skinnedMeshRendy != null) return p.colliderForSkinnedMesh;//skinnedMeshRendy.sharedMesh;
        if (p.meshFilter != null) return p.meshFilter.sharedMesh;

        return null;
    }

        public static bool ContainsInstanceType(this List<PainterPluginBase> collection, Type type){

		foreach (var t in collection) 
			if (t.GetType() == type) return true; 
		
		return false;
	}

		public static float StrokeWidth (this BrushConfig br, float pixWidth, bool world) => br.Size(world) / (pixWidth) * 2 * PainterCamera.orthoSize;
		

        public static bool IsSingleBufferBrush(this BrushConfig b) => (PainterCamera.Inst.isLinearColorSpace && b.BlitMode.SupportedBySingleBuffer && b.Type(false).SupportedBySingleBuffer && b.PaintingRGB);
        
        
        public static bool IsProjected(this Material mat)
        {
            if (mat == null) return false;
            return mat.shaderKeywords.Contains(PainterDataAndConfig.UV_PROJECTED);
        }

        public static StdEncoder EncodeStrokeFor(this BrushConfig brush, PlaytimePainter painter) {
         


            var id = painter.ImgData;

            bool rt = id.TargetIsRenderTexture();

            BlitMode mode = brush.BlitMode;
            BrushType type = brush.Type(!rt);

            bool worldSpace = rt && brush.IsA3Dbrush(painter);
            
            StdEncoder cody = new StdEncoder()

            .Add(rt ? "typeGPU" : "typeCPU", brush._type(!rt));

            if (worldSpace)
                cody.Add("size3D", brush.Brush3D_Radius);
            else
                cody.Add("size2D", brush.Brush2D_Radius/((float)id.width));


            cody.Add_Bool("useMask", brush.useMask)
            .Add("mode", brush._bliTMode);

            if (brush.useMask)
                cody.Add("mask", (int)brush.mask);

        

            if (mode.ShowColorSliders)
                cody.Add("bc", brush.colorLinear);

            if (mode.UsingSourceTexture)
                cody.Add("source", brush.selectedSourceTexture);

            if (rt) {

                if ((mode.GetType() == typeof(BlitModeBlur)))
                    cody.Add("blur", brush.blurAmount);

                if (type.IsUsingDecals) {
                    cody.Add("decA", brush.decalAngle)
                    .Add("decNo", brush.selectedDecal);
                }

                if (brush.useMask) {
                    cody.Add("Smask", brush.selectedSourceMask)
                    .Add("maskTil", brush.maskTiling)
                    .Add_Bool("maskFlip", brush.flipMaskAlpha)
                    .Add("maskOff", brush.maskOffset);
                }
            }

            cody.Add("hard",brush.Hardness)
            .Add("speed", brush.speed);
     
            return cody;
        }
        
        public static bool NeedsGrid (this PlaytimePainter pntr) {
            if (pntr == null || !pntr.enabled) return false;
            
            if (!pntr.meshEditing) {

                if (!pntr.LockTextureEditing && !PainterCamera.Data.showConfig && PlaytimePainter.IsCurrent_Tool()) {
                    if (pntr.GlobalBrushType.NeedsGrid) return true;

                    if (GridNavigator.pluginNeedsGrid_Delegates != null)
                    foreach (PainterBoolPlugin p in GridNavigator.pluginNeedsGrid_Delegates.GetInvocationList())
                        if (p(pntr)) return true;
                }
                return false;
            }
            else return PainterCamera.Inst.meshManager.target == pntr && PainterCamera.Data.MeshTool.ShowGrid;
        }

        public static void RemoveEmpty(this Dictionary<string, List<ImageData>> dic)
        {
            foreach (KeyValuePair<string, List<ImageData>> l in dic)
                l.Value.RemoveEmpty();
        }
        
        public static void AddIfNew(this Dictionary<string, List<ImageData>> dic, string Property, ImageData texture)
        {

            List<ImageData> mgmt;
            if (!dic.TryGetValue(Property, out mgmt))
            {
                mgmt = new List<ImageData>();
                dic.Add(Property, mgmt);
            }

            if (!mgmt.ContainsDuplicant(texture))
                mgmt.Add(texture);

        }

        public static bool TargetIsTexture2D(this ImageData id)
        {
            if (id == null) return false;
            return id.destination == TexTarget.Texture2D;
        }

        public static bool TargetIsRenderTexture(this ImageData id)
        {
            if (id == null) return false;
            return id.destination == TexTarget.RenderTexture;
        }

        public static bool TargetIsBigRenderTexture(this ImageData id)
        {
            if (id == null) return false;
            return (id.destination == TexTarget.RenderTexture) && (id.renderTexture == null);
        }

        public static ImageData EnsureStaticInstance(this ImageData imgDTA)
        {

            if (imgDTA == null)
                return null;

            ImageData id = null;
            if (imgDTA.texture2D)
                id = imgDTA.texture2D.GetImgDataIfExists();
            else if (imgDTA.renderTexture)
                id = imgDTA.renderTexture.GetImgDataIfExists();
            else if (imgDTA.other != null)
                id = imgDTA.other.GetImgDataIfExists();
            else
                return null;

            if (id == null)
            {
                PainterCamera.Data.imgDatas.Add(imgDTA);
                id = imgDTA;
            }

            return id;
        }

        public static ImageData GetImgDataIfExists(this Texture texture)
        {
            if (texture == null)
                return null;

            if (texture.IsBigRenderTexturePair() && PainterCamera.Inst.imgDataUsingRendTex != null)
                return PainterCamera.Inst.imgDataUsingRendTex;

            ImageData rid = null;

            var lst = PainterCamera.Data.imgDatas;

            for (int i = 0; i < lst.Count; i++) {
                ImageData id = lst[i];
                if ((texture == id.texture2D) || (texture == id.renderTexture) || (texture == id.other)) {
                    rid = id;
                    if (i > 3) 
                        PainterCamera.Data.imgDatas.Move(i, 0);
                    break;
                }
            }

            return rid;
        }
        
        public static ImageData GetImgData(this Texture texture)
        {
            if (texture == null)
                return null;

            var nid = texture.GetImgDataIfExists();

            if (nid == null)
                nid = new ImageData().Init(texture);
            
            return nid;
        }

        public static bool IsBigRenderTexturePair(this Texture tex)
        {
            return ((tex != null) && PainterCamera.GotBuffers() && ((tex == PainterCamera.Inst.BigRT_pair[0])));
        }

        public static bool ContainsDuplicant(this List<ImageData> texs, ImageData other)
        {

            if (other == null)
                return true;

            for (int i = 0; i < texs.Count; i++)
                if (texs[i] == null) { texs.RemoveAt(i); i--; }

            foreach (ImageData t in texs)
                if (t.Equals(other))
                    return true;

            return false;
        }

        public static Texture GetDestinationTexture(this Texture texture)
        {

            ImageData id = texture.GetImgDataIfExists();
            if (id != null)
                return id.CurrentTexture();

            return texture;
        }

        public static RenderTexture CurrentRenderTexture(this ImageData id)
        {
            if (id == null)
                return null;
            return id.renderTexture ?? PainterCamera.Inst.BigRT_pair[0];
        }

        public static Texture ExclusiveTexture(this ImageData id)
        {
            if (id == null)
                return null;

            if (id.other != null)
                return id.other;

            switch (id.destination)
            {
                case TexTarget.RenderTexture:
                    return id.renderTexture == null ? (Texture)id.texture2D : (Texture)id.renderTexture;
                case TexTarget.Texture2D:
                    return id.texture2D;
            }
            return null;
        }

        public static Texture CurrentTexture(this ImageData id)
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
                    if (PainterCamera.Inst.imgDataUsingRendTex == id)
                        return PainterCamera.Inst.BigRT_pair[0];
                    id.destination = TexTarget.Texture2D;
                    return id.texture2D;
                case TexTarget.Texture2D:
                    return id.texture2D;
            }
            return null;
        }

        public static MaterialData GetMaterialData (this Material mat) {
            return  PainterCamera.Data?.GetMaterialDataFor(mat);
        }


    }

}