using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;


namespace Playtime_Painter{

	public abstract class BlitMode  : IeditorDropdown  {

		private static List<BlitMode> _allModes;

		protected PainterManager rt {get{ return PainterManager.inst;}}

        public int index;

		protected static PlaytimePainter painter;
        public static BrushConfig pegibrush;

		public static BlitMode getCurrentBlitModeForPainter (PlaytimePainter inspectedPainter) 
		{ painter = inspectedPainter; return PainterConfig.inst.brushConfig.blitMode; }

        public static List<BlitMode> allModes {  get {
				if (_allModes == null)
					InstantiateBrushes ();
				return _allModes; } 
		} 

		public virtual bool showInDropdown(){
            if (painter == null)  
                return (pegibrush.TargetIsTex2D ? supportedByTex2D : supportedByRenderTexturePair);

			imgData id = painter.curImgData;

			if (id == null)
				return false;

			return  ((id.destination == texTarget.Texture2D) && (supportedByTex2D)) || 
				(
					(id.destination == texTarget.RenderTexture) && 
					((supportedByRenderTexturePair && (id.renderTexture == null)) 
						|| (supportedBySingleBuffer && (id.renderTexture != null)))
				);
		}

		public BlitMode setKeyword (){

			foreach (BlitMode bs in allModes) 
				if (bs != this) {
					string name = bs.shaderKeyword;
					if (name != null)
						BlitModeExtensions.KeywordSet (name,false);
				}

			if (shaderKeyword!= null)
				BlitModeExtensions.KeywordSet (shaderKeyword,true);

            return this;

		}

		protected virtual string shaderKeyword { get {return null;}}

		public virtual void SetGlobalShaderParameters(){
			

			
		}

        public BlitMode() {
            index = _allModes.Count;
        }

		protected static void InstantiateBrushes() {

			_allModes = new List<BlitMode> ();


         
            _allModes.Add (new BlitModeAlphaBlit());
            _allModes.Add (new BlitModeAdd());
            _allModes.Add (new BlitModeSubtract());
            _allModes.Add (new BlitModeCopy());
            _allModes.Add(new BlitModeMin());
            _allModes.Add(new BlitModeMax());
            _allModes.Add(new BlitModeBlur());
            _allModes.Add(new BlitModeBloom());

            // The code below uses reflection to find all classes that are child classes of BlitMode.
            // The code above adds them manually to save some compilation time,
            // and if you add new BlitMode, just add _allModes.Add (new BlitModeMyStuff ());
            // Alternatively, in a far-far future, the code below may be reanabled if there will be like hundreds of fan-made brushes for my asset
            // Which would be cool, but I'm a realist so whatever happens its ok, I lived a good life and greatfull for every day.

            /*
			List<Type> allTypes = CsharpFuncs.GetAllChildTypesOf<BlitMode>();
			foreach (Type t in allTypes) {
				BlitMode tb = (BlitMode)Activator.CreateInstance(t);
				_allModes.Add(tb);
			}
            */
        }

        public virtual blitModeFunction BlitFunctionTex2D { get{ return Blit_Functions.AlphaBlit; }}

		public virtual bool supportedByTex2D {get { return true; }}
		public virtual bool supportedByRenderTexturePair {get { return true; }}
		public virtual bool supportedBySingleBuffer {get { return true; }}
		public virtual bool usingSourceTexture {get {return false;}}
		public virtual bool showColorSliders {get { return true;}}
		public virtual Shader shaderForDoubleBuffer { get { return rt.br_Multishade; } }
		public virtual Shader shaderForSingleBuffer { get { return rt.br_Blit; } }

		public virtual bool PEGI(BrushConfig brush, PlaytimePainter p){

            imgData id = p == null ? null : p.curImgData;

            bool cpuBlit = id == null ? brush.TargetIsTex2D : id.destination == texTarget.Texture2D;
            BrushType brushType = brush.type;
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

         

            if ((!cpuBlit) && brushType.isA3Dbrush) {

                Mesh m = painter.getMesh() ;

                float maxScale = (m != null ? m.bounds.max.magnitude : 1) * (painter == null ? 1 : painter.transform.lossyScale.magnitude);
               
                changed |= pegi.edit(ref brush.Brush3D_Radius, 0.001f * maxScale, maxScale * 0.5f);
               

            }
            else
            {
                if (!brushType.isPixelPerfect)
                    changed |= pegi.edit(ref brush.Brush2D_Radius, cpuBlit ? 1 : 0.1f, usingDecals ? 128 : (id != null ? id.width * 0.5f : 256));
                else {
                    int val = (int)brush.Brush2D_Radius;
                    changed |= pegi.edit(ref val, (int) (cpuBlit ? 1 : 0.1f), (int)(usingDecals ? 128 : (id != null ? id.width * 0.5f : 256)));
                    brush.Brush2D_Radius = val;
                }
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
        static BlitModeAdd _inst;
        public static BlitModeAdd inst { get { if (_inst == null) InstantiateBrushes(); return  _inst; } }

		public override string ToString () { return "Add";}
		protected override string shaderKeyword { get { return "BRUSH_ADD";} }

        public override Shader shaderForSingleBuffer {get {return rt.br_Add;}}
		public override blitModeFunction BlitFunctionTex2D {get { return Blit_Functions.AddBlit; } }

        public BlitModeAdd() {
            _inst = this;
        }
	}

    public class BlitModeSubtract : BlitMode {
        public override string ToString() { return "Subtract"; }
        protected override string shaderKeyword { get { return "BRUSH_SUBTRACT"; } }

        //public override Shader shaderForSingleBuffer { get { return rt.br_Add; } }
        public override bool supportedBySingleBuffer { get { return false; } }

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
		public override bool supportedByRenderTexturePair { get { return false; } }
		public override bool supportedBySingleBuffer { get { return false; } }
		public override blitModeFunction BlitFunctionTex2D {get { return Blit_Functions.MinBlit; } }
	}

	public class BlitModeMax : BlitMode {
		public override string ToString () { return "Max";}
		public override bool supportedByRenderTexturePair { get { return false; } }
		public override bool supportedBySingleBuffer { get { return false; } }
		public override blitModeFunction BlitFunctionTex2D {get { return Blit_Functions.MaxBlit; } }
	}

	public class BlitModeBlur : BlitMode {
		public override string ToString () { return "Blur";}
		protected override string shaderKeyword { get { return "BRUSH_BLUR";} }
		public override bool showColorSliders {get {return false;}}
		public override bool supportedBySingleBuffer { get { return false; } }
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
		public override bool supportedBySingleBuffer { get { return false; } }
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