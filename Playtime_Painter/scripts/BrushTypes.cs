using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace Painter{

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

	protected static PainterManager rtp {get{ return PainterManager.inst;}}
	protected static Transform rtbrush { get { return rtp.brushRendy.transform; } }
	protected static Mesh brushMesh { set { rtp.brushRendy.meshFilter.mesh = value; } }

	private static List<BrushType> _allTypes;

	protected static PlaytimePainter painter;

	public static BrushType getCurrentBrushTypeForPainter (PlaytimePainter inspectedPainter) 
		{ painter = inspectedPainter; return PainterConfig.inst.brushConfig.currentBrushTypeRT(); } 

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

    public Vector2 uvToPosition(Vector2 uv)
        {
            return (uv - Vector2.one * 0.5f) * PainterManager.orthoSize * 2;

            //Vector2 meshPos = ((st.uvFrom + st.uvTo) - Vector2.one) * rtp.orthoSize;
        }

    public Vector2 to01space(Vector2 from)
        {
            from.x %= 1;
            from.y %= 1;
            if (from.x < 0) from.x += 1;
            if (from.y < 0) from.y += 1;
            return from;
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
					|| (supportedBySingleBuffer && (id.renderTexture != null)))
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
	public virtual bool supportedBySingleBuffer {get { return true; }}
	public virtual bool isA3Dbrush {get { return false;}}
	public virtual bool isUsingDecals {get { return false;}}
	public virtual bool startPaintingTheMomentMouseIsDown {get { return true;}}
    public virtual bool supportedForTerrain_RT { get { return true; } }

	public virtual bool PEGI(BrushConfig brush){
		
            bool change = false;
		pegi.newLine();
		if (PainterManager.inst.masks.Length > 0) {

			brush.selectedSourceMask = Mathf.Clamp(brush.selectedSourceMask, 0,PainterManager.inst.masks.Length-1); 
				
			pegi.Space();
			pegi.newLine();

			change |= pegi.toggle(ref brush.useMask, "Mask","Multiply Brush Speed By Mask Texture's alpha", 40);

			//pegi.newLine();

			if (brush.useMask) {
				
				pegi.selectOrAdd(ref brush.selectedSourceMask, ref PainterManager.inst.masks);

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

                    if (PainterConfig.inst.moreOptions || brush.flipMaskAlpha)
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

			if (rtp.BigRT_pair == null) rtp.UpdateBuffersState();

			imgData id = painter.curImgData;

			rtp.ShaderPrepareStroke_UpdateBuffer(br, br.speed*0.05f, id);

			bool isSphere = br.currentBrushTypeRT().isA3Dbrush;

			

			if (isSphere) {

				Vector3 hitPos = st.posTo;
				if (st.mouseDwn)
					st.posFrom = hitPos;

                Vector2 offset = id.offset.To01Space();

                st.SetWorldPosInShader();

				Shader.SetGlobalVector(PainterConfig.BRUSH_EDITED_UV_OFFSET, new Vector4(id.tiling.x, id.tiling.y, offset.x, offset.y));

				rtp.brushRendy.UseMeshAsBrush(pntr);
				rtp.Render();
			

			}
			else {
              

                rtbrush.localScale = Vector3.one;
				Vector2 direction = st.delta_uv;
				float length = direction.magnitude;
				brushMesh = brushMeshGenerator.inst().GetLongMesh(length * 256, br.strokeWidth(id.width, false));
				rtbrush.localRotation = Quaternion.Euler(new Vector3(0, 0, (direction.x > 0 ? -1 : 1) * Vector2.Angle(Vector2.up, direction)));
				
				rtbrush.localPosition =  StrokeVector.brushWorldPositionFrom((st.uvFrom+st.uvTo)/2);

                rtp.Render();

                if (!br.isSingleBufferBrush())
                    rtp.UpdateBufferSegment();
                
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

        public static void Paint (Vector2 uv, BrushConfig br, RenderTexture rt) {

            if (rtp.BigRT_pair == null) rtp.UpdateBuffersState();

            var id = rt.getImgData();

            rtp.ShaderPrepareStroke_UpdateBuffer(br, br.speed * 0.05f, id);

            float width = br.strokeWidth(id.width, false);

            rtbrush.localScale = Vector3.one;
               
            brushMesh = brushMeshGenerator.inst().GetLongMesh(0, width);
            rtbrush.localRotation = Quaternion.Euler(new Vector3(0, 0, Vector2.Angle(Vector2.up, Vector2.zero)));

            rtbrush.localPosition = StrokeVector.brushWorldPositionFrom(uv);

            rtp.Render();

            if (!br.isSingleBufferBrush())
               rtp.UpdateBufferSegment();

        }

}

public class BrushTypeDecal : BrushType {

        static BrushTypeDecal _inst;
        public BrushTypeDecal() { _inst = this; }
        public static BrushTypeDecal inst { get { initIfNull(); return _inst; } }
        public override bool supportedBySingleBuffer { get { return false; } }
        protected override string shaderKeyword { get {return "BRUSH_DECAL";} }
	    public override bool isUsingDecals { get { return true; } }

	    public override string ToString ()
	{
		return "Decal";
	}

        Vector2 previousUV;

		public override void Paint (PlaytimePainter pntr, BrushConfig br, StrokeVector st) {
		
		painter = pntr;
		
			imgData id = pntr.curImgData;

			if ((st.firstStroke) || (br.decalContinious)) {
                
                if (br.decalRotationMethod == DecalRotationMethod.StrokeDirection)
                {
                    Vector2  delta = st.uvTo - previousUV;

                   // if ((st.firstStroke) || (delta.magnitude*id.width > br.Size(false)*0.25f)) {

                    float portion = Mathf.Clamp01( delta.magnitude * id.width * 4 / br.Size(false));

                    float newAngle = Vector2.SignedAngle(Vector2.up, delta) + br.decalAngleModifier;
                    br.decalAngle = Mathf.LerpAngle(br.decalAngle, newAngle, portion);

                         previousUV = st.uvTo;
                    //}
                    
                }

				if (rtp.BigRT_pair == null) rtp.UpdateBuffersState();

			

				rtp.ShaderPrepareStroke_UpdateBuffer(br, 1, id);
				Transform tf = rtbrush;
                tf.localScale = Vector3.one * br.Size(false);
                tf.localRotation = Quaternion.Euler(new Vector3(0, 0, br.decalAngle));
                brushMesh = brushMeshGenerator.inst().GetQuad();

                st.uvTo = st.uvTo.To01Space();

                Vector2 deltauv = st.delta_uv;

                /* 

                 int strokes = Mathf.Max(1, (br.decalContinious && (!st.firstStroke)) ? (int)(deltauv.magnitude*id.width/br.Size(false)) : 1);

                 deltauv /=  strokes;

                 for (int i = 0; i < strokes; i++) {
                     st.uvFrom += deltauv;*/

                Vector2 uv = st.uvTo;

                if ((br.decalRotationMethod == DecalRotationMethod.StrokeDirection) && (!st.firstStroke)) {
                    float length = Mathf.Max(deltauv.magnitude * 2* id.width / br.Size(false), 1);
                    Vector3 scale = tf.localScale;

                    if (( Mathf.Abs(Mathf.Abs(br.decalAngleModifier) - 90)) < 40)
                        scale.x *= length;
                    else
                        scale.y *= length;

                    tf.localScale = scale;
                    uv -= deltauv * ((length - 1)*0.5f/length);
                }

                    tf.localPosition = StrokeVector.brushWorldPositionFrom(uv);


                // if (strokes > 1) Debug.Log("Stroke " + i + ":" + tf.localPosition);

                rtp.Render();//Render_UpdateSecondBufferIfUsing(id);

                if (!br.isSingleBufferBrush())
                    rtp.UpdateBufferSegment();


                if (br.decalRotationMethod == DecalRotationMethod.Random) {
                        br.decalAngle = UnityEngine.Random.Range(-90f, 450f);
                        pntr.Update_Brush_Parameters_For_Preview_Shader();
                    }
              //  }
              
            }
			painter.AfterStroke (st);
	}

		public override bool PEGI (BrushConfig br) {
		
		bool brushChanged_RT = false;
		brushChanged_RT |= pegi.select<VolumetricDecal>(ref br.selectedDecal, PainterManager.inst.decals);
		pegi.newLine();

		if (PainterManager.inst.GetDecal(br.selectedDecal) == null)
			pegi.write("Select valid decal; Assign to Painter Camera.");
		pegi.newLine();

        "Continious".toggle("Will keep adding decal every frame while the mouse is down", 80, ref br.decalContinious).nl();

        "Rotation".write("Rotation method", 60);

        br.decalRotationMethod = (DecalRotationMethod)pegi.editEnum<DecalRotationMethod>(br.decalRotationMethod); // "Random Angle", 90);
		pegi.newLine();
		if (br.decalRotationMethod == DecalRotationMethod.Set) {
			"Angle:".write ("Decal rotation", 60);
			brushChanged_RT |= pegi.edit(ref br.decalAngle, -90, 450);
		} else if (br.decalRotationMethod == DecalRotationMethod.StrokeDirection) {
                "Ang Offset:".edit("Angle modifier after the rotation method is applied",80, ref br.decalAngleModifier, -180f, 180f); 
        } 

		pegi.newLine();
		if (!br.mask.GetFlag(BrushMask.A))
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

		//	Vector2 outb = new Vector2(Mathf.Floor(st.uvTo.x), Mathf.Floor(st.uvTo.y));
		//	st.uvTo -= outb;
		//	st.uvFrom -= outb;

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
				PainterManager r = rtp;
			//RenderTexturePainter.inst.RenderLazyBrush(painter.Previous_uv, uv, brush.speed * 0.05f, painter.curImgData, brush, painter.LmouseUP, smooth );
				if (rtp.BigRT_pair == null) rtp.UpdateBuffersState();

				imgData id = painter.curImgData;

				float meshWidth = br.strokeWidth(id.width, false); //.Size(false) / ((float)id.width) * 2 * rtp.orthoSize;

				Transform tf = rtbrush;

				Vector2 direction = st.delta_uv;//uvTo - uvFrom;

				bool isTail = st.firstStroke;//(!previousTo.Equals(uvFrom));

				if ((!isTail) && (!smooth)) {
                    //Debug.Log ("Junction point");
                    r.ShaderPrepareStroke_UpdateBuffer(br, br.speed * 0.05f, id);

                    Vector3 junkPoint = st.uvFrom + st.previousDelta * 0.01f;
					brushMesh = brushMeshGenerator.inst().GetStreak(uvToPosition(st.uvFrom), uvToPosition(junkPoint), meshWidth, true, false);
					tf.localScale = Vector3.one;
					tf.localRotation = Quaternion.identity;
					tf.localPosition = new Vector3(0, 0, 10);


                    r.Render();//Render_UpdateSecondBufferIfUsing(id);
					st.uvFrom = junkPoint;
					isTail = true;
				}

                r.ShaderPrepareStroke_UpdateBuffer(br, br.speed * 0.05f, id);

                brushMesh = brushMeshGenerator.inst().GetStreak(uvToPosition(st.uvFrom), uvToPosition(st.uvTo), meshWidth, st.mouseUp, isTail);
                tf.localScale = Vector3.one;
				tf.localRotation = Quaternion.identity;
				tf.localPosition = new Vector3(0, 0, 10);

				st.previousDelta = direction;

                r.Render();//Render_UpdateSecondBufferIfUsing(id);

                if (!br.isSingleBufferBrush())
                    rtp.UpdateBufferSegment();

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


        static void PrepareSphereBrush(imgData id, BrushConfig br, StrokeVector st) {
            if (rtp.BigRT_pair == null) rtp.UpdateBuffersState();

            if (st.mouseDwn)
                st.posFrom = st.posTo;

            rtp.ShaderPrepareStroke_UpdateBuffer(br, br.speed * 0.05f, id);

            Vector2 offset = id.offset.To01Space();

            st.SetWorldPosInShader();

            Shader.SetGlobalVector(PainterConfig.BRUSH_EDITED_UV_OFFSET, new Vector4(id.tiling.x, id.tiling.y, offset.x, offset.y));
        }


        public override void Paint(PlaytimePainter pntr, BrushConfig br, StrokeVector st)  {

            painter = pntr;

            imgData id = pntr.curImgData;

            PrepareSphereBrush(id, br, st);

            if (!st.mouseDwn) {
                rtp.brushRendy.UseMeshAsBrush(pntr);
                rtp.Render();  
               
            }

            pntr.AfterStroke(st);
        }

        public static void Paint(RenderTexture rt, GameObject go, SkinnedMeshRenderer skinner, BrushConfig br, Vector3 pos) {
            PrepareSphereBrush(rt.getImgData(), br, new StrokeVector(pos));
            rtp.brushRendy.UseSkinMeshAsBrush(go, skinner);
            rtp.Render();
        }

        public static void Paint(RenderTexture rt, GameObject go, Mesh mesh, BrushConfig br, Vector3 pos) {
            PrepareSphereBrush(rt.getImgData(), br, new StrokeVector(pos));
            rtp.brushRendy.UseMeshAsBrush(go,mesh);
            rtp.Render();
        }
        }
}