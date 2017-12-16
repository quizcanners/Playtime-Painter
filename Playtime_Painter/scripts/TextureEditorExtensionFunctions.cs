using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
#if UNITY_EDITOR
using UnityEditor;
#endif

using StoryTriggerData;

namespace Painter{

public static class TextureEditorExtensionFunctions  {


    public static Mesh getMesh(this PlaytimePainter p) {
        if (p == null) return null;
        if (p.skinnedMeshRendy != null) return p.colliderForSkinnedMesh;//skinnedMeshRendy.sharedMesh;
        if (p.meshFilter != null) return p.meshFilter.sharedMesh;

        return null;
    }

    public static bool ContainsInstanceType(this List<NonMaterialTexture> collection, Type type){

		foreach (var t in collection) 
			if (t.GetType() == type) return true; 
		
		return false;
	}



		public static bool ColorSliders(this BrushConfig b, PlaytimePainter painter)  {
			bool changed = false;
				bool slider = b.currentBlitMode ().showColorSliders;

			if ((painter!=null) && (painter.isTerrainHeightTexture())) {
				changed |= b.ColorSlider (BrushMask.A, ref b.color.a, null,true);
				//b.MaskSet (BrushMask.R, false);
				//b.MaskSet (BrushMask.G, false);
				//b.MaskSet (BrushMask.B, false);

			}
			else if ((painter != null) && painter.isTerrainControlTexture())
			{
				// Debug.Log("Is control texture");
				changed |= b.ColorSlider(BrushMask.R, ref b.color.r, painter.terrain.getSplashPrototypeTexture(0),slider);
				changed |= b.ColorSlider(BrushMask.G, ref b.color.g, painter.terrain.getSplashPrototypeTexture(1),slider);
				changed |= b.ColorSlider(BrushMask.B, ref b.color.b, painter.terrain.getSplashPrototypeTexture(2),slider);
				changed |= b.ColorSlider(BrushMask.A, ref b.color.a, painter.terrain.getSplashPrototypeTexture(3),slider);
			}
			else
			{

				if ((painter.curImgData.TargetIsRenderTexture()) && (painter.curImgData.renderTexture != null)) {
					changed |= b.ColorSlider (BrushMask.R, ref b.color.r);
					changed |= b.ColorSlider (BrushMask.G, ref b.color.g);
					changed |= b.ColorSlider (BrushMask.B, ref b.color.b);

				} else {
					
					changed |= b.ColorSlider (BrushMask.R, ref b.color.r, null,slider);
					changed |= b.ColorSlider (BrushMask.G, ref b.color.g, null,slider);
					changed |= b.ColorSlider (BrushMask.B, ref b.color.b, null,slider);
					changed |= b.ColorSlider (BrushMask.A, ref b.color.a, null,slider);
				}
			}
			return changed;
		}

		public static float strokeWidth (this BrushConfig br, float pixWidth, bool world){
			return br.Size(world) / (pixWidth) * 2 * PainterManager.orthoSize;
		}

		public static BrushType currentBrushTypeRT(this BrushConfig cfg) { return BrushType.allTypes [cfg.brushType_rt]; }
		public static BlitMode currentBlitMode(this BrushConfig cfg) { BlitMode.pegibrush = cfg; return BlitMode.allModes [cfg.bliTMode]; }

        public static stdEncoder EncodeStrokeFor(this BrushConfig brush, PlaytimePainter painter) {
            stdEncoder cody = new stdEncoder();

            bool rt = painter.curImgData.TargetIsRenderTexture();

            BlitMode mode = brush.currentBlitMode();
            BrushType type = brush.currentBrushTypeRT();


            if (rt) cody.Add("typeRT", brush.brushType_rt);
            else cody.AddText("typeCPU", "cpu");

            bool worldSpace = rt && type.isA3Dbrush;

            if (worldSpace)
                cody.Add("size3D", brush.Brush3D_Radius);
            else
                cody.Add("size2D", brush.Brush2D_Radius/((float)painter.curImgData.width));


            cody.Add("useMask", brush.useMask);

            if (brush.useMask)
                cody.Add("mask", (int)brush.mask);

            cody.Add("mode", brush.bliTMode);

            if (mode.showColorSliders)
                cody.AddIfNotNull(brush.color);

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
            cody.Add("smooth", brush.Smooth);


            return cody;
        }

        public static float Size(this BrushConfig brush, imgData id) {
            bool worldSpace = id.TargetIsRenderTexture() && brush.currentBrushTypeRT().isA3Dbrush;
            return brush.Size(worldSpace);
        }

}

}