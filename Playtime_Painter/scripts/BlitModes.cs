using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;


namespace TextureEditor{

	public abstract class BlitMode  : IeditorDropdown  {

		private static List<BlitMode> _allModes;

		protected RenderTexturePainter rt {get{ return RenderTexturePainter.inst;}}

		protected static PlaytimePainter painter;
        public static BrushConfig pegibrush;

		public static BlitMode getCurrentBlitModeForPainter (PlaytimePainter inspectedPainter) 
		{ painter = inspectedPainter; return painterConfig.inst().brushConfig.currentBlitMode(); }

        public static List<BlitMode> allModes {  get {
				if (_allModes == null)
					InstantiateBrushes ();
				return _allModes; } 
		} 

		public virtual bool showInDropdown(){
            if (painter == null)  
                return (pegibrush.IndependentCPUblit ? supportedByTex2D : supportedByBigRT);

			imgData id = painter.curImgData;

			if (id == null)
				return false;

			return  ((id.destination == texTarget.Texture2D) && (supportedByTex2D)) || 
				(
					(id.destination == texTarget.RenderTexture) && 
					((supportedByBigRT && (id.renderTexture == null)) 
						|| (supportedByExclusiveRT && (id.renderTexture != null)))
				);
		}

		public void setKeyword (){

			foreach (BlitMode bs in allModes) 
				if (bs != this) {
					string name = bs.shaderKeyword;
					if (name != null)
						BlitModeExtensions.KeywordSet (name,false);
				}

			if (shaderKeyword!= null)
				BlitModeExtensions.KeywordSet (shaderKeyword,true);

		}

		protected virtual string shaderKeyword { get {return null;}}

		public virtual void SetGlobalShaderParameters(){
			

			
		}

		static void InstantiateBrushes() {

			_allModes = new List<BlitMode> ();

			/*
		 The code under this comment uses reflection to find all classes that are child classes of BrushTypesBase.
		 This could be done manually like this:
			_allTypes.Add (new BrushTypeNormal ());
			_allTypes.Add (new BrushTypeDecal ());
			_allTypes.Add (new BrushTypeLazy ());
			_allTypes.Add (new BrushTypeSphere ());
		This could save some compilation time for larger projects, and if you add new brush, just add _allTypes.Add (new BrushTypeMyNewBrush ());
		*/

			List<Type> allTypes = CsharpFuncs.GetAllChildTypesOf<BlitMode>();
			foreach (Type t in allTypes) {
				BlitMode tb = (BlitMode)Activator.CreateInstance(t);
				_allModes.Add(tb);
			}			
		}

		public virtual blitModeFunction BlitFunctionTex2D { get{ return Blit_Functions.AlphaBlit; }}



		public virtual bool supportedByTex2D {get { return true; }}
		public virtual bool supportedByBigRT {get { return true; }}
		public virtual bool supportedByExclusiveRT {get { return true; }}
		public virtual bool usingSourceTexture {get {return false;}}
		public virtual bool showColorSliders {get { return true;}}
		public virtual Shader shaderForDoubleBuffer { get { return rt.br_Multishade; } }
		public virtual Shader shaderForSingleBuffer { get { return rt.br_Blit; } }

		public virtual bool PEGI(BrushConfig brush, PlaytimePainter p){

            imgData id = p == null ? null : p.curImgData;

            bool cpuBlit = id == null ? brush.IndependentCPUblit : id.destination == texTarget.Texture2D;
            BrushType brushType = brush.currentBrushTypeRT();
            bool usingDecals = (!cpuBlit) && brushType.isUsingDecals; 


            bool changed = false;

            pegi.newLine();

            if ((!cpuBlit) && (!usingDecals))
            {
                pegi.write("Hardness:", "Makes edges more rough.", 70);
                changed |= pegi.edit(ref brush.Hardness, 1f, 512f);
                pegi.newLine();
            }

            pegi.write(usingDecals ? "Tint alpha" : "Speed", usingDecals ? 70 : 40);
            changed |= pegi.edit(ref brush.speed, 0.01f, 20);
            pegi.newLine();
            pegi.write("Scale:", 40);
            if ((!cpuBlit) && brushType.isA3Dbrush)
            {

                Mesh m = painter.getMesh() ;

                float maxScale = (m != null ? m.bounds.max.magnitude : 1) * (painter == null ? 128 : painter.transform.lossyScale.magnitude);
                changed |= pegi.edit(ref brush.Brush3D_Radius, 0.001f * maxScale, maxScale * 0.5f);

            }
            else
            {

                changed |= pegi.edit(ref brush.Brush2D_Radius, cpuBlit ? 1 : 0.1f, usingDecals ? 128 : (id != null ? id.width * 0.5f : 256));
            }

            pegi.newLine();

            return changed;
		}
}

	public class BlitModeAlphaBlit : BlitMode {
		public override string ToString () { return "Alpha Blit";}
		protected override string shaderKeyword { get { return "BRUSH_NORMAL";} }
	}

	public class BlitModeAdd : BlitMode {
		public override string ToString () { return "Add";}
		protected override string shaderKeyword { get { return "BRUSH_ADD";} }

		public override Shader shaderForSingleBuffer {get {return rt.br_Add;}}
		public override blitModeFunction BlitFunctionTex2D {get { return Blit_Functions.AddBlit; } }
	}

    public class BlitModeSubtract : BlitMode {
        public override string ToString() { return "Subtract"; }
        protected override string shaderKeyword { get { return "BRUSH_SUBTRACT"; } }

        //public override Shader shaderForSingleBuffer { get { return rt.br_Add; } }
        public override bool supportedByExclusiveRT { get { return false; } }

        public override blitModeFunction BlitFunctionTex2D { get { return Blit_Functions.SubtractBlit; } }
    }

	public class BlitModeCopy : BlitMode {
		public override string ToString () { return "Copy";}
		protected override string shaderKeyword { get { return "BRUSH_COPY";} }
		public override bool showColorSliders {get {return false;}}

		public override bool supportedByTex2D { get { return false; } }
		public override bool usingSourceTexture { get {return true;} }
		public override Shader shaderForSingleBuffer {get {return rt.br_Copy;}}
	}

	public class BlitModeMin : BlitMode {
		public override string ToString () { return "Min";}
		public override bool supportedByBigRT { get { return false; } }
		public override bool supportedByExclusiveRT { get { return false; } }
		public override blitModeFunction BlitFunctionTex2D {get { return Blit_Functions.MinBlit; } }
	}

	public class BlitModeMax : BlitMode {
		public override string ToString () { return "Max";}
		public override bool supportedByBigRT { get { return false; } }
		public override bool supportedByExclusiveRT { get { return false; } }
		public override blitModeFunction BlitFunctionTex2D {get { return Blit_Functions.MaxBlit; } }
	}

	public class BlitModeBlur : BlitMode {
		public override string ToString () { return "Blur";}
		protected override string shaderKeyword { get { return "BRUSH_BLUR";} }
		public override bool showColorSliders {get {return false;}}
		public override bool supportedByExclusiveRT { get { return false; } }
		public override bool supportedByTex2D { get { return false; } }

		public override Shader shaderForDoubleBuffer {get {return rt.br_BlurN_SmudgeBrush;}}

		public override bool PEGI (BrushConfig brush, PlaytimePainter p) {

            bool brushChanged_RT = base.PEGI(brush, p);
			pegi.newLine ();
				pegi.write ("Blur Amount", 70);
				brushChanged_RT |=pegi.edit (ref brush.blurAmount,1f,8f);
			pegi.newLine ();
			return brushChanged_RT;
		}
	}

	public class BlitModeBloom : BlitMode {
		public override string ToString () { return "Bloom";}
		protected override string shaderKeyword { get { return "BRUSH_BLOOM";} }

		public override bool showColorSliders {get {return false;}}
		public override bool supportedByExclusiveRT { get { return false; } }
		public override bool supportedByTex2D { get { return false; } }

		public override Shader shaderForDoubleBuffer {get {return rt.br_BlurN_SmudgeBrush;}}

		public override bool PEGI (BrushConfig brush, PlaytimePainter p) {
			
			bool brushChanged_RT = base.PEGI(brush, p);
            pegi.newLine ();
			pegi.write ("Bloom Radius", 70);
			brushChanged_RT |=pegi.edit (ref brush.blurAmount,1f,8f);
			pegi.newLine ();
			return brushChanged_RT; 
		}
	}

}