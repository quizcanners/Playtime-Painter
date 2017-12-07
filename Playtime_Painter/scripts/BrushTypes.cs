using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace TextureEditor{

public static class BlitModeExtensions {
	public static void KeywordSet (string name, bool to){
		if (to)
			Shader.EnableKeyword (name);
		else
			Shader.DisableKeyword (name);
	}

		public static void SetShaderToggle ( bool value, string iftrue, string iffalse){
			Shader.DisableKeyword(value ? iffalse : iftrue);
			Shader.EnableKeyword(value ?  iftrue : iffalse);
		}



}

public abstract class BrushType : IeditorDropdown {

	protected RenderTexturePainter rtp {get{ return RenderTexturePainter.inst;}}
	protected Transform rtbrush { get { return rtp.brushRendy.transform; } }
	protected Mesh brushMesh { set { rtp.brushRendy.meshFilter.mesh = value; } }

	private static List<BrushType> _allTypes;

	protected static PlaytimePainter painter;

	public static BrushType getCurrentBrushTypeForPainter (PlaytimePainter inspectedPainter) 
		{ painter = inspectedPainter; return painterConfig.inst().brushConfig.currentBrushTypeRT(); } 

	public static List<BrushType> allTypes {  get {  initIfNull(); return _allTypes; } 
	} 
    
    public static void initIfNull() {
            if (_allTypes != null) return;

                _allTypes = new List<BrushType>();
            /*
             The code under this comment uses reflection to find all classes that are child classes of BrushTypesBase.
             This could be done manually like this:
                _allTypes.Add (new BrushTypeNormal ());
                _allTypes.Add (new BrushTypeDecal ());
                _allTypes.Add (new BrushTypeLazy ());
                _allTypes.Add (new BrushTypeSphere ());
            This could save some compilation time for larger projects, and if you add new brush, just add _allTypes.Add (new BrushTypeMyNewBrush ());
            */

            List<Type> allTypes = CsharpFuncs.GetAllChildTypesOf<BrushType>();
            foreach (Type t in allTypes)
            {
                BrushType tb = (BrushType)Activator.CreateInstance(t);
                _allTypes.Add(tb);
            }
        }

	public virtual bool showInDropdown(){
		if (painter == null)
			return false;
		
		imgData id = painter.curImgData;

		if (id == null)
			return false;

		return  //((id.destination == dest.Texture2D) && (supportedByTex2D)) || 
			
			(((id.destination == texTarget.RenderTexture) && 
				((supportedByBigRT && (id.renderTexture == null)) 
					|| (supportedByExclusiveRT && (id.renderTexture != null)))
			));
	}

	public void setKeyword (){
		
		foreach (BrushType bs in allTypes) 
			if (bs != this) {
			string name = bs.shaderKeyword;
			if (name != null)
				BlitModeExtensions.KeywordSet (name,false);
		}
		
		if (shaderKeyword!= null)
			BlitModeExtensions.KeywordSet (shaderKeyword,true);
		
	}

	protected virtual string shaderKeyword { get {return null;}}

    static int typesCount = 0;
    public int index;
       

    public BrushType() {
            index = typesCount;
            typesCount++;
           // Debug.Log("Type "+index);
        }
        

    public virtual bool supportedByBigRT {get { return true; }}
	public virtual bool supportedByExclusiveRT {get { return true; }}
	public virtual bool isA3Dbrush {get { return false;}}
	public virtual bool isUsingDecals {get { return false;}}
	public virtual bool startPaintingTheMomentMouseIsDown {get { return true;}}
    public virtual bool supportedForTerrain_RT { get { return true; } }

	public virtual bool PEGI(BrushConfig brush){
		
            bool change = false;
		pegi.newLine();
		if (RenderTexturePainter.inst.masks.Length > 0) {

			brush.selectedSourceMask = Mathf.Clamp(brush.selectedSourceMask, 0,RenderTexturePainter.inst.masks.Length-1); 
				
			pegi.Space();
			pegi.newLine();

			change |= pegi.toggle(ref brush.useMask, "Mask","Multiply Brush Speed By Mask Texture's alpha", 40);

			//pegi.newLine();

			if (brush.useMask) {
				
				pegi.selectOrAdd(ref brush.selectedSourceMask, ref RenderTexturePainter.inst.masks);

				pegi.newLine();

                    if (!brush.randomMaskOffset) {

                        pegi.write("Mask Offset: ", 70);

                        change |= pegi.edit(ref brush.maskOffset);

                        pegi.newLine();
                    }

                    pegi.write("Random Mask Offset");

                    change |= pegi.toggle(ref brush.randomMaskOffset);

                    pegi.newLine();

				pegi.write("Mask Tiling: ", 70);

				if (pegi.edit(ref brush.maskTiling, 1, 8)) {
					brush.maskTiling = Mathf.Clamp(brush.maskTiling, 0.1f, 64);
					change = true;
				}

                    pegi.newLine();

                    if (painterConfig.inst().moreOptions || brush.flipMaskAlpha)
                        change |= pegi.toggle(ref brush.flipMaskAlpha, "Flip Mask Alpha", "Alpha = 1-Alpha");

                    pegi.newLine();
			}
		}
		else { pegi.writeHint("Assign some Masks to Painter Camera"); }

		return change;
	}

	public virtual void Paint (PlaytimePainter pntr, BrushConfig br, StrokeVector st){
		
		painter = pntr;

            if (st.crossedASeam())
				st.uvFrom = st.uvTo;

		Vector2 outb = new Vector2(Mathf.Floor(st.uvTo.x), Mathf.Floor(st.uvTo.y));
		st.uvFrom -= outb;
		st.uvFrom -= outb;

			if (rtp.BigRT_pair == null) rtp.UpdateBuffersState();

			imgData id = painter.curImgData;

			rtp.ShaderPrepareStroke(br, br.speed*0.05f, id);

			bool isSphere = br.currentBrushTypeRT().isA3Dbrush;

			float width = br.strokeWidth (id.width, isSphere);//.Size(isSphere) / ((float)id.width) * 2 * r.orthoSize;

			if (isSphere) {

               

				Vector3 hitPos = st.posTo;
				if (st.mouseDwn)
					st.posFrom = hitPos;

				Vector2 offset = rtp.to01space(id.offset);

				Shader.SetGlobalVector("_brushWorldPosFrom", new Vector4(st.posFrom.x, st.posFrom.y, st.posFrom.z, 0));
				Shader.SetGlobalVector("_brushWorldPosTo", new Vector4(hitPos.x, hitPos.y, hitPos.z, st.delta_WorldPos.magnitude));
				Shader.SetGlobalVector("_brushEditedUVoffset", new Vector4(id.tyling.x, id.tyling.y, offset.x, offset.y));
				st.posFrom = hitPos;

				rtp.brushRendy.CopyAllFrom(pntr.gameObject, pntr.getMesh());
				rtp.Render();

				rtp.brushRendy.RestoreBounds();

				if ((RenderTexturePainter.GotBuffers ()) && (id.currentRenderTexture () == rtp.BigRT_pair [0])) {
					rtp.PrepareFullCopyBrush(rtp.BigRT_pair[0]);
					rtp.UpdateBufferTwo ();
				}
			}
			else {


				rtbrush.localScale = Vector3.one;
				Vector2 direction = st.delta_uv;
				float length = direction.magnitude;
				brushMesh = brushMeshGenerator.inst().GetLongMesh(length * 256, width);
				rtbrush.localRotation = Quaternion.Euler(new Vector3(0, 0, (direction.x > 0 ? -1 : 1) * Vector2.Angle(Vector2.up, direction)));
				
				rtbrush.localPosition =  st.brushWorldPositionFrom((st.uvFrom+st.uvTo)/2);
				rtp.Render_UpdateSecondBufferIfUsing(id);
			}



		pntr.AfterStroke(st);
	}

}

public class BrushTypeNormal : BrushType {

        static BrushTypeNormal _inst;
        public BrushTypeNormal() { _inst = this; }
        public static BrushTypeNormal inst { get { initIfNull(); return _inst;} }

        protected override string shaderKeyword { get { return "BRUSH_2D"; } }

	public override string ToString ()
	{
		return "Normal";
	}

}

public class BrushTypeDecal : BrushType {

        static BrushTypeDecal _inst;
        public BrushTypeDecal() { _inst = this; }
        public static BrushTypeDecal inst { get { initIfNull(); return _inst; } }

        protected override string shaderKeyword { get {return "BRUSH_DECAL";} }
	public override bool isUsingDecals { get { return true; } }

	public override string ToString ()
	{
		return "Decal";
	}

		public override void Paint (PlaytimePainter pntr, BrushConfig br, StrokeVector st) {
		
		painter = pntr;
		
			imgData id = pntr.curImgData;

			if ((st.firstStroke) || (br.decalContinious)) {
				

				if (rtp.BigRT_pair == null) rtp.UpdateBuffersState();

				st.uvTo = rtp.to01space(st.uvTo);

				rtp.ShaderPrepareStroke(br, 1, id);

				Transform tf = rtbrush;

				tf.localScale = Vector3.one * br.Size(false);
				brushMesh = brushMeshGenerator.inst().GetQuad();
				tf.localRotation = Quaternion.Euler(new Vector3(0, 0, br .decalAngle));
				st.uvFrom = st.uvTo;
				tf.localPosition = st.brushWorldPositionFrom(st.uvTo);
				rtp.Render_UpdateSecondBufferIfUsing(id);
				if (br.decalRandomRotation) {
					br.decalAngle = UnityEngine.Random.Range (-90f, 450f);
					pntr.Update_Brush_Parameters_For_Preview_Shader ();
				}
			}
			painter.AfterStroke (st);
	}

		public override bool PEGI (BrushConfig br) {
		
		bool brushChanged_RT = false;
		brushChanged_RT |= pegi.select<VolumetricDecal>(ref br.selectedDecal, RenderTexturePainter.inst.decals);
		pegi.newLine();

		if (RenderTexturePainter.inst.GetDecal(br.selectedDecal) == null)
			pegi.write("Select valid decal; Assign to Painter Camera.");
		pegi.newLine();

		pegi.toggle(ref br.decalContinious, "Continious", "Will keep adding decal every frame while the mouse is down", 80);
		pegi.newLine();
		pegi.toggle(ref br.decalRandomRotation, "Random Angle", 90);
		pegi.newLine();
		if (br.decalRandomRotation == false) {
			pegi.write("Angle:", "Decal rotation", 60);
			brushChanged_RT |= pegi.edit(ref br.decalAngle, -90, 450);
		}
		pegi.newLine();
		if (!br.GetMask(BrushMask.A))
			pegi.writeHint("! Alpha chanel is disabled. Decals may not render properly");

		return brushChanged_RT;

	}

}

public class BrushTypeLazy : BrushType {

        static BrushTypeLazy _inst;
        public BrushTypeLazy() { _inst = this; }
        public static BrushTypeLazy inst { get { initIfNull(); return _inst; } }

        public override bool startPaintingTheMomentMouseIsDown { get {return false;}}
	protected override string shaderKeyword {get {return "BRUSH_2D";}}

	public override string ToString () {
		return "Lazy";
	}

	public float LazySpeedDynamic = 1;
	public float LazyAngleSmoothed = 1;
	Vector2 previousDirectionLazy;

	public override void Paint (PlaytimePainter pntr, BrushConfig br, StrokeVector st) {
		painter = pntr;

			Vector2 outb = new Vector2(Mathf.Floor(st.uvTo.x), Mathf.Floor(st.uvTo.y));
			st.uvTo -= outb;
			st.uvFrom -= outb;

		Vector2 delta_uv = st.delta_uv;//uv - st.uvFrom;//.Previous_uv;
		float magn = delta_uv.magnitude;

		float width = br.Size(false) / ((float)painter.curImgData.width) * 4;
            //const float followPortion = 0.5f;
            //float follow = width;

            float trackPortion = (delta_uv.magnitude - width * 0.5f) * 0.25f;

            if ((trackPortion > 0) || (st.mouseUp)) {

               

				if (st.firstStroke) {
				previousDirectionLazy = st.previousDelta =  delta_uv;
				LazySpeedDynamic = delta_uv.magnitude;
				LazyAngleSmoothed = 0;
                   // Debug.Log("First stroke");
			}

                float angle = Mathf.Deg2Rad * Vector2.Angle(st.previousDelta, delta_uv);

                bool smooth = angle < Mathf.PI * 0.5f;

            if ((st.crossedASeam()) && (magn > previousDirectionLazy.magnitude * 8)) 
                {
                   // Debug.Log("Crossed a seam");
				st.mouseUp = true;
					st.uvTo = st.uvFrom;// painter.Previous_uv;
				delta_uv = Vector2.zero;
                    smooth = false;
			}

			previousDirectionLazy = delta_uv;

		

			if (!st.mouseUp) {
				if (smooth) {
					float clockwise = Vector3.Cross(st.previousDelta, delta_uv).z > 0 ? 1 : -1;
					float sin = Mathf.Sin(angle) * clockwise;
					float maxSinus = 8;
					if (Mathf.Abs(LazyAngleSmoothed) > Mathf.Abs(sin)) LazyAngleSmoothed = sin;
					else
						LazyAngleSmoothed = Mathf.Lerp(LazyAngleSmoothed, sin, 0.2f);
					sin = LazyAngleSmoothed;

					if ((sin * sin > maxSinus * maxSinus) || ((sin > 0) != (maxSinus > 0))) {

						float absSin = Mathf.Abs(sin);
						float absNSin = Mathf.Abs(maxSinus);

						if (absSin < absNSin) maxSinus = maxSinus * absSin / absNSin;

                            st.uvTo  = st.uvFrom + st.previousDelta.normalized.Rotate(maxSinus * clockwise) * trackPortion;
                            LazySpeedDynamic = trackPortion;
					} else {
						LazySpeedDynamic = Mathf.Min(delta_uv.magnitude * 0.5f, Mathf.Lerp(LazySpeedDynamic, delta_uv.magnitude * 0.5f, 0.001f));

                            LazySpeedDynamic = Mathf.Max(trackPortion, LazySpeedDynamic);
							st.uvTo = st.uvFrom  + st.previousDelta.normalized.Rotate(sin) * LazySpeedDynamic;
					}
				} else {
					LazySpeedDynamic = delta_uv.magnitude;
					LazyAngleSmoothed = 0;
                        st.uvTo = st.uvFrom + delta_uv.normalized * trackPortion;
				}
			}
				RenderTexturePainter r = rtp;
			//RenderTexturePainter.inst.RenderLazyBrush(painter.Previous_uv, uv, brush.speed * 0.05f, painter.curImgData, brush, painter.LmouseUP, smooth );
				if (rtp.BigRT_pair == null) rtp.UpdateBuffersState();

				imgData id = painter.curImgData;

				float meshWidth = br.strokeWidth(id.width, false); //.Size(false) / ((float)id.width) * 2 * rtp.orthoSize;

				Transform tf = rtbrush;

				Vector2 direction = st.delta_uv;//uvTo - uvFrom;

				bool isTail = st.firstStroke;//(!previousTo.Equals(uvFrom));

				if ((!isTail) && (!smooth)) {
					//Debug.Log ("Junction point");

					Vector3 junkPoint = st.uvFrom + st.previousDelta * 0.01f;
					brushMesh = brushMeshGenerator.inst().GetStreak(r.uvToPosition(st.uvFrom), r.uvToPosition(junkPoint), meshWidth, true, false);
					tf.localScale = Vector3.one;
					tf.localRotation = Quaternion.identity;
					tf.localPosition = new Vector3(0, 0, 10);

					r.ShaderPrepareStroke(br, br.speed*0.05f, id);
					r.Render_UpdateSecondBufferIfUsing(id);
					st.uvFrom = junkPoint;
					isTail = true;
				}

				brushMesh = brushMeshGenerator.inst().GetStreak(r.uvToPosition(st.uvFrom), r.uvToPosition(st.uvTo), meshWidth, st.mouseUp, isTail);

				tf.localScale = Vector3.one;
				tf.localRotation = Quaternion.identity;
				tf.localPosition = new Vector3(0, 0, 10);

				st.previousDelta = direction;
				r.ShaderPrepareStroke(br, br.speed*0.05f, id);
				r.Render_UpdateSecondBufferIfUsing(id);

			painter.AfterStroke(st);
		}
	}
}

public class BrushTypeSphere : BrushType {

        static BrushTypeSphere _inst;
        public BrushTypeSphere() { _inst = this; }
        public static BrushTypeSphere inst { get { initIfNull(); return _inst; } }

        public override bool isA3Dbrush { get { return true; } }
        public override bool supportedForTerrain_RT {  get { return false; } }

        protected override string shaderKeyword { get { return "BRUSH_3D";} }

	public override string ToString () {
		return "Sphere";
	}

        public override void Paint(PlaytimePainter pntr, BrushConfig br, StrokeVector st)  {

            painter = pntr;

            if (rtp.BigRT_pair == null) rtp.UpdateBuffersState();

            imgData id = pntr.curImgData;

            Vector3 hitPos = st.posTo;

            if (st.mouseDwn)
                st.posFrom = hitPos;

                Vector2 offset = rtp.to01space(id.offset);

                Shader.SetGlobalVector("_brushWorldPosFrom", new Vector4(st.posFrom.x, st.posFrom.y, st.posFrom.z, 0));
                Shader.SetGlobalVector("_brushWorldPosTo", new Vector4(hitPos.x, hitPos.y, hitPos.z, st.delta_WorldPos.magnitude));
                Shader.SetGlobalVector("_brushEditedUVoffset", new Vector4(id.tyling.x, id.tyling.y, offset.x, offset.y));
                st.posFrom = hitPos;

                rtp.brushRendy.CopyAllFrom(pntr.gameObject, pntr.getMesh());

            rtp.ShaderPrepareStroke(br, br.speed * 0.05f, id);

           if (!st.mouseDwn)
            rtp.Render();   // Disabling brush Renderer clears texture Right Away

            rtp.brushRendy.RestoreBounds();



               if ((RenderTexturePainter.GotBuffers()) && (id.currentRenderTexture() == rtp.BigRT_pair[0])) {
                    rtp.PrepareFullCopyBrush(rtp.BigRT_pair[0]);
                    rtp.UpdateBufferTwo();
               // Debug.Log("Painting sphere");
                }
            
            pntr.AfterStroke(st);
        }


    }
}