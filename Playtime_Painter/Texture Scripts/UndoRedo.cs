using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using SharedTools_Stuff;


namespace Playtime_Painter {


    public class TextureBackup {
        public int order;
        public List<string> strokeRecord;

        protected virtual void SetB(ImageData from, int globalOrder) {
            order = globalOrder;
            strokeRecord = from.recordedStrokes_forUndoRedo;
            from.recordedStrokes_forUndoRedo = new List<string>();

        }
    }

    public class Texture2DBackup : TextureBackup {
		public Color[] pixels;

        public void Set(Color[] texturePixels,ImageData from, int globalOrder){

            SetB(from,globalOrder);
			pixels = texturePixels;

          
		}

        public Texture2DBackup (Color[] texturePixels, ImageData from, int globalOrder){
            Set (texturePixels, from, globalOrder);
		}

	}

    public class RenderTextureBackup : TextureBackup {
		public RenderTexture rt;
        public bool exclusive;

		public void Set (ImageData from, int globalOrder){
          
            PainterCamera.Inst.Blit(from.CurrentRenderTexture(), rt);

            SetB(from, globalOrder);

            exclusive = from.renderTexture != null;
		}

        public RenderTextureBackup (ImageData from, int globalOrder){
			RenderTexture frt = from.CurrentRenderTexture ();

			rt = new RenderTexture(from.width, from.height, 0, RenderTextureFormat.ARGB32, 
				frt.sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
			rt.filterMode = frt.filterMode;
            Set (from, globalOrder);
		}

		public void DestroyRtex(){
			UnityHelperFunctions.DestroyWhatever(rt);

		}

	}

	
	public class BackupsLineup {
		public static PainterCamera rtp { get { return PainterCamera.Inst; } }
        public bool isUndo;
		public int order = 0;

		public List<Texture2DBackup> tex2D = new List<Texture2DBackup>();
		public List<RenderTextureBackup> rtex = new List<RenderTextureBackup>();	

		public BackupsLineup otherDirection;

		public string CurrentStep = "";

		/*public void Transfer (Texture2DBackup bu){
			bu.order = order;
			order++;
			tex2D.Add (bu);
		}

		public void Transfer (RenderTextureBackup bu){
			bu.order = order;
			order++;
			rtex.Add (bu);
		}*/

		public bool gotData (){
			return ((tex2D.Count > 0) || (rtex.Count>0));
		}

		public void Clear(){
			foreach (RenderTextureBackup r in rtex)
				r.DestroyRtex ();
		
			tex2D.Clear ();
			rtex.Clear ();
		}

		public void ClearRtexUpto(int maxTextures){
			int toclear = rtex.Count - maxTextures;

			for (int i = 0; i < toclear; i++)
				rtex [i].DestroyRtex ();

			rtex.SetMaximumLength (maxTextures);
		}



		public void ApplyTo (ImageData id) {

			bool fromRT = (tex2D.Count == 0) || ((rtex.Count > 0) && (tex2D [tex2D.Count - 1].order < rtex [rtex.Count - 1].order));

			bool toRT = id.destination == TexTarget.RenderTexture;

            int toClear = id.recordedStrokes_forUndoRedo.Count;

            if (toRT) 
                otherDirection.backupRenderTexture(int.MaxValue, id);
             else 
                otherDirection.backupTexture2D(int.MaxValue, id);

           
            RenderTextureBackup rtBackup = fromRT ? takeRenderTexture () : null;
            Texture2DBackup pixBackup = fromRT ? null : takeTexture2D ();
            TextureBackup backup = fromRT ? (TextureBackup)rtBackup : (TextureBackup)pixBackup;


           
            if (!isUndo)
                id.recordedStrokes.AddRange(backup.strokeRecord);
            else 
                id.recordedStrokes.RemoveLast(toClear);
            
            id.recordedStrokes_forUndoRedo = backup.strokeRecord;

          /*  if (isUndo) {
             
               
            } else {
                id.recordedStrokes.AddRange(backup.strokeRecord);
                id.recordedStrokes_forUndoRedo.AddRange(backup.strokeRecord);
            }*/


			if (!fromRT) {
                id.Pixels = (Color[])pixBackup.pixels;//.Clone();
				id.SetAndApply (true);
			}

			if (toRT) {
				if (fromRT) 
					rtp.Render (rtBackup.rt, id);
				else 
					rtp.Render (id.texture2D, id);
				
			} else if (fromRT) {
					id.texture2D.CopyFrom (rtBackup.rt);
                    id.PixelsFromTexture2D (id.texture2D);

                bool converted = false;

                if ((PainterCamera.Inst.isLinearColorSpace) && (!rtBackup.exclusive))
                {
                    converted = true;
                    id.PixelsToGamma();
                }
                //else
                //   id.pixelsToLinear();
                // if (!RenderTexturePainter.inst.isLinearColorSpace)
                //{
                //  Debug.Log("Pixels to lnear");

                //   id.pixelsToLinear();
                //}

                // In Linear dont turn to gamma if saved from Exclusive Render Texture
                if (converted)
                    id.SetAndApply(true);
                else
                    id.texture2D.Apply(true);
            } 

			if (fromRT)
				rtBackup.DestroyRtex ();


                
			
		}

		Texture2DBackup takeTexture2D (){
			int index = tex2D.Count - 1;
			Texture2DBackup pixels = tex2D [index];
			tex2D.RemoveAt (index);
			return pixels;
		}

		RenderTextureBackup takeRenderTexture (){
			int index = rtex.Count - 1;
			RenderTextureBackup rt = rtex [index];
			rtex.RemoveAt (index);
			return rt;
		}


        public void backupTexture2D (int maxTextures, ImageData id){

			tex2D.SetMaximumLength (maxTextures);

			if (maxTextures > 0) {
				
				Color[] copyPix = (Color[])id.Pixels.Clone ();

				if (tex2D.Count < maxTextures)
                    tex2D.Add (new Texture2DBackup (copyPix, id, order));
				else 
                    tex2D.MoveFirstToLast ().Set (copyPix, id, order);

                id.recordedStrokes_forUndoRedo = new List<string>();

				order++;
			}

		}

		public void backupRenderTexture (int maxTextures, ImageData from){

			ClearRtexUpto (maxTextures);

			if (maxTextures > 0) {
				
				if (rtex.Count < maxTextures)
                    rtex.Add (new RenderTextureBackup (from, order));
				else 
                    rtex.MoveFirstToLast ().Set (from, order);



				order++;
			}

		}

        public BackupsLineup(bool undo) {
            isUndo = undo;
        }

	}


	public class UndoCache {

		public BackupsLineup undo;
		public BackupsLineup redo;

		public UndoCache (){
			undo = new BackupsLineup(true);
			redo = new BackupsLineup(false);

			undo.otherDirection = redo;
			redo.otherDirection = undo;
		}

	}

}