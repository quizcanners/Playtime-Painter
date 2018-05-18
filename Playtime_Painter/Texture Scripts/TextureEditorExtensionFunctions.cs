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

        public static Mesh getMesh(this PlaytimePainter p) {
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

		public static float strokeWidth (this BrushConfig br, float pixWidth, bool world){
			return br.Size(world) / (pixWidth) * 2 * PainterManager.orthoSize;
		}

        public static bool isSingleBufferBrush(this BrushConfig b) { 
                return (PainterManager.inst.isLinearColorSpace && b.blitMode.supportedBySingleBuffer && b.type(false).supportedBySingleBuffer && b.paintingRGB);
        }
        
        public static bool isProjected(this Material mat)
        {
            if (mat == null) return false;
            return mat.shaderKeywords.Contains(PainterConfig.UV_PROJECTED);
        }

        public static stdEncoder EncodeStrokeFor(this BrushConfig brush, PlaytimePainter painter) {
            stdEncoder cody = new stdEncoder();

            var id = painter.imgData;

            bool rt = id.TargetIsRenderTexture();

            BlitMode mode = brush.blitMode;
            BrushType type = brush.type(!rt);
            
            cody.Add(rt ? "typeGPU" : "typeCPU", brush._type(!rt));
            
            bool worldSpace = rt && brush.IsA3Dbrush(painter);

            if (worldSpace)
                cody.Add("size3D", brush.Brush3D_Radius);
            else
                cody.Add("size2D", brush.Brush2D_Radius/((float)id.width));


            cody.Add("useMask", brush.useMask);

            if (brush.useMask)
                cody.Add("mask", (int)brush.mask);

            cody.Add("mode", brush._bliTMode);

            if (mode.showColorSliders)
                cody.Add("bc", brush.colorLinear);

            if (mode.usingSourceTexture)
                cody.Add("source", brush.selectedSourceTexture);

            if (rt) {

                if ((mode.GetType() == typeof(BlitModeBlur)))
                    cody.Add("blur", brush.blurAmount);

                if (type.isUsingDecals) {
                    cody.Add("decA", brush.decalAngle);
                    cody.Add("decNo", brush.selectedDecal);
                }

                if (brush.useMask) {
                    cody.Add("Smask", brush.selectedSourceMask);
                    cody.Add("maskTil", brush.maskTiling);
                    cody.Add("maskFlip", brush.flipMaskAlpha);
                    cody.Add("maskOff", brush.maskOffset);
                }
            }



            cody.Add("hard",brush.Hardness);
            cody.Add("speed", brush.speed);
          


            return cody;
        }
        
        public static bool needsGrid (this PlaytimePainter pntr) {
            if (pntr == null || !pntr.enabled) return false;
            
            if (!pntr.meshEditing) {

                if (!pntr.LockTextureEditing && !PainterConfig.inst.showConfig && PlaytimePainter.isCurrent_Tool()) {
                    if (pntr.globalBrushType.needsGrid) return true;

                    if (GridNavigator.pluginNeedsGrid_Delegates != null)
                    foreach (PainterBoolPlugin p in GridNavigator.pluginNeedsGrid_Delegates.GetInvocationList())
                        if (p(pntr)) return true;
                }
                return false;
            }
            else return PainterManager.inst.meshManager.target == pntr && PainterConfig.inst.meshTool.showGrid;
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
            return id.destination == texTarget.Texture2D;
        }

        public static bool TargetIsRenderTexture(this ImageData id)
        {
            if (id == null) return false;
            return id.destination == texTarget.RenderTexture;
        }

        public static bool TargetIsBigRenderTexture(this ImageData id)
        {
            if (id == null) return false;
            return (id.destination == texTarget.RenderTexture) && (id.renderTexture == null);
        }

        public static ImageData getImgDataIfExists(this Texture texture)
        {
            if (texture == null)
                return null;

            if (texture.isBigRenderTexturePair() && PainterManager.inst.imgDataUsingRendTex != null)
                return PainterManager.inst.imgDataUsingRendTex;

            ImageData rid = null;

            var lst = PainterManager.inst.imgDatas;

            for (int i = 0; i < lst.Count; i++) {
                ImageData id = lst[i];
                if ((texture == id.texture2D) || (texture == id.renderTexture)) {
                    rid = id;
                    if (i > 3) 
                        PainterManager.inst.imgDatas.Move(i, 0);
                    break;
                }
            }



            return rid;
        }

        //static ImageData recentImgDta;
        //static Texture recentTexture;
        public static ImageData getImgData(this Texture texture)
        {
            if (texture == null)
                return null;

          //  if (recentTexture != null && texture == recentTexture && recentImgDta != null)
            //    return recentImgDta;

            //Debug.Log("Looping trough texture datas");

            var nid = texture.getImgDataIfExists();

            if (nid == null)
            //{
              //  Debug.Log("Creating imgDATA for " + texture.name);
                nid = ScriptableObject.CreateInstance<ImageData>().init(texture);
            //}
            //else Debug.Log("Returning for "+texture.name);

            //recentImgDta = nid;
            //recentTexture = texture;

            return nid;
        }

        public static bool isBigRenderTexturePair(this Texture tex)
        {
            return ((tex != null) && PainterManager.GotBuffers() && ((tex == PainterManager.inst.BigRT_pair[0])));
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

        public static Texture getDestinationTexture(this Texture texture)
        {

            ImageData id = texture.getImgDataIfExists();
            if (id != null)
                return id.currentTexture();

            return texture;
        }

        public static RenderTexture currentRenderTexture(this ImageData id)
        {
            if (id == null)
                return null;
            return id.renderTexture == null ? PainterManager.inst.BigRT_pair[0] : id.renderTexture;
        }

        public static Texture exclusiveTexture(this ImageData id)
        {
            if (id == null)
                return null;
            switch (id.destination)
            {
                case texTarget.RenderTexture:
                    return id.renderTexture == null ? (Texture)id.texture2D : (Texture)id.renderTexture;
                case texTarget.Texture2D:
                    return id.texture2D;
            }
            return null;
        }

        public static Texture currentTexture(this ImageData id)
        {
            if (id == null)
                return null;
            switch (id.destination)
            {
                case texTarget.RenderTexture:
                    if (id.renderTexture != null)
                        return id.renderTexture;
                    if (PainterManager.inst.imgDataUsingRendTex == id)
                        return PainterManager.inst.BigRT_pair[0];
                    id.destination = texTarget.Texture2D;
                    return id.texture2D;
                case texTarget.Texture2D:
                    return id.texture2D;
            }
            return null;
        }

        public static MaterialData GetMaterialData (this Material mat) {
            return PainterManager.inst.getMaterialDataFor(mat);
        }


    }

}