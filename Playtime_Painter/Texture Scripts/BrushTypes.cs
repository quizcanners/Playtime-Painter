using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace Playtime_Painter{

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

public abstract class BrushType : PainterStuff, IeditorDropdown {

	/*protected static PainterManager mgmt {get{ return PainterManager.inst;}}
	protected static Transform rtbrush { get { return mgmt.brushRendy.transform; } }
	protected static Mesh brushMesh { set { mgmt.brushRendy.meshFilter.mesh = value; } }
    protected static PainterConfig cfg { get { return PainterConfig.inst; } }
    protected static BrushConfig brush { get { return BrushConfig.inspectedBrush; } }
    protected static PlaytimePainter painter { get { return PlaytimePainter.inspectedPainter; } }*/


    private static List<BrushType> _allTypes;

	public static List<BrushType> allTypes {  get {  initIfNull(); return _allTypes; } 
	} 
    
    public static void initIfNull() {
            if (_allTypes != null) return;

                _allTypes = new List<BrushType>();
            
                _allTypes.Add (new BrushTypeNormal ());
                _allTypes.Add (new BrushTypeDecal ());
                _allTypes.Add (new BrushTypeLazy ());
                _allTypes.Add (new BrushTypeSphere ());
                _allTypes.Add(new BrushTypePixel());
               // _allTypes.Add(new BrushTypeSamplingOffset());
          
            //The code below can find all brushes, but at some Compilation time cost:
            /*
            List<Type> allTypes = CsharpFuncs.GetAllChildTypesOf<BrushType>();
            foreach (Type t in allTypes)
            {
                BrushType tb = (BrushType)Activator.CreateInstance(t);
                _allTypes.Add(tb);
            }
            */
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
		if (PlaytimePainter.inspectedPainter == null)
			return false;
		
		imgData id = PlaytimePainter.inspectedPainter.curImgData;

		if (id == null)
			return false;

		return  //((id.destination == dest.Texture2D) && (supportedByTex2D)) || 
			
			(((id.destination == texTarget.RenderTexture) && 
				((supportedByRenderTexturePair && (id.renderTexture == null)) 
					|| (supportedBySingleBuffer && (id.renderTexture != null)))
			));
	}

	public void setKeyword (bool texcoord2){
		
		foreach (BrushType bs in allTypes) {
			string name = bs.shaderKeyword(true);
			if (name != null)
                    Shader.DisableKeyword(name);

            name = bs.shaderKeyword(false);
            if (name != null)
                    Shader.DisableKeyword(name);
            }
		
		if (shaderKeyword(texcoord2) != null)
			BlitModeExtensions.KeywordSet (shaderKeyword(texcoord2),true);
		
	}

	protected virtual string shaderKeyword (bool texcoord2) {  return null;}

    static int typesCount = 0;
    public int index;

    public BrushType() {
            index = typesCount;
            typesCount++;
        }
        
    public virtual bool supportedByTex2D { get { return false; } }
    public virtual bool supportedByRenderTexturePair {get { return true; }}
	public virtual bool supportedBySingleBuffer {get { return true; }}
	public virtual bool isA3Dbrush {get { return false;}}
    public virtual bool isPixelPerfect { get { return false; }}
	public virtual bool isUsingDecals {get { return false;}}
	public virtual bool startPaintingTheMomentMouseIsDown {get { return true;}}
    public virtual bool supportedForTerrain_RT { get { return true; } }
    public virtual bool useGrid { get { return false; } }

	public virtual bool PEGI(){
		
            bool change = false;
		pegi.newLine();

        if (BrushConfig.inspectedIsCPUbrush)
            return change;
            
        if  (PainterManager.inst.masks.Length > 0) {

			inspectedBrush.selectedSourceMask = Mathf.Clamp(inspectedBrush.selectedSourceMask, 0,PainterManager.inst.masks.Length-1); 
				
			pegi.Space();
			pegi.newLine();

			change |= pegi.toggle(ref inspectedBrush.useMask, "Mask","Multiply Brush Speed By Mask Texture's alpha", 40);

			//pegi.newLine();

			if (inspectedBrush.useMask) {
				
				pegi.selectOrAdd(ref inspectedBrush.selectedSourceMask, ref PainterManager.inst.masks);

				pegi.newLine();

                    if (!inspectedBrush.randomMaskOffset) {

                        pegi.write("Mask Offset: ", 70);

                        change |= pegi.edit(ref inspectedBrush.maskOffset);

                        pegi.newLine();
                    }

                    pegi.write("Random Mask Offset");

                    change |= pegi.toggle(ref inspectedBrush.randomMaskOffset);

                    pegi.newLine();

				pegi.write("Mask Tiling: ", 70);

				if (pegi.edit(ref inspectedBrush.maskTiling, 1, 8)) {
					inspectedBrush.maskTiling = Mathf.Clamp(inspectedBrush.maskTiling, 0.1f, 64);
					change = true;
				}

                    pegi.newLine();

                    if (PainterConfig.inst.moreOptions || inspectedBrush.flipMaskAlpha)
                        change |= pegi.toggle(ref inspectedBrush.flipMaskAlpha, "Flip Mask Alpha", "Alpha = 1-Alpha");

                    pegi.newLine();
			}


		}
		else { pegi.writeHint("Assign some Masks to Painter Camera"); pegi.newLine(); }

        

            if (useGrid && "Center Grid".Click().nl())
                GridNavigator.onGridPos = painter.transform.position;

		return change;
	}

    public virtual void PaintToTexture2D(PlaytimePainter pntr, BrushConfig br, StrokeVector st)
        {

            Vector2 delta_uv = st.uvTo - st.uvFrom;
            
            if (delta_uv.magnitude > (0.2f + st.avgBrushSpeed * 3)) delta_uv = Vector2.zero; // This is made to avoid glitch strokes on seams
            else st.avgBrushSpeed = (st.avgBrushSpeed + delta_uv.magnitude) / 2;

            float dist = (int)(delta_uv.magnitude * pntr.curImgData.width * 8 / br.Size(false));

            dist = Mathf.Max(dist, 1);
            delta_uv /= dist;
            float alpha = Mathf.Clamp01(br.speed * (Application.isPlaying ? Time.deltaTime : 0.1f));
            st.uvFrom += delta_uv;

            PainterPluginBase pluginBlit = null;

            float startI = 0;

            foreach (var p in pntr.plugins)
                if (p.PaintTexture2D(st.uvFrom, alpha, pntr.curImgData, br)) {
                    pluginBlit = p;
                    startI = 1;
                    break;
                }
            
            for (float i = startI; i < dist; i++) {
                if (pluginBlit != null)
                    pluginBlit.PaintTexture2D(st.uvFrom, alpha, pntr.curImgData, br);
                else
                    Blit_Functions.Paint(st.uvFrom, alpha, pntr.curImgData, br);
                st.uvFrom += delta_uv;
            }

            pntr.AfterStroke(st);
        }

    public virtual void Paint (PlaytimePainter pntr, BrushConfig br, StrokeVector st){

            BeforeStroke(pntr, br, st);

            if (st.crossedASeam())
                st.uvFrom = st.uvTo;

			if (texMGMT.BigRT_pair == null) texMGMT.UpdateBuffersState();

			imgData id = pntr.curImgData;

			texMGMT.ShaderPrepareStroke(br, br.speed*0.05f, id, st.useTexcoord2, st.firstStroke);

	
              

                rtbrush.localScale = Vector3.one;
				Vector2 direction = st.delta_uv;
				float length = direction.magnitude;
				brushMesh = brushMeshGenerator.inst().GetLongMesh(length * 256, br.strokeWidth(id.width, false));
				rtbrush.localRotation = Quaternion.Euler(new Vector3(0, 0, (direction.x > 0 ? -1 : 1) * Vector2.Angle(Vector2.up, direction)));
				
				rtbrush.localPosition =  StrokeVector.brushWorldPositionFrom((st.uvFrom+st.uvTo)/2);

                texMGMT.Render();

            AfterStroke(pntr, br, st);
        }

    protected static void AfterStroke(BrushConfig br) {

            if ((br.useMask) && (br.randomMaskOffset))
                br.maskOffset = new Vector2(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));

        }

    protected virtual void BeforeStroke(PlaytimePainter pntr, BrushConfig br, StrokeVector st)
        {
            foreach (var p in pntr.plugins)
                p.BeforeGPUStroke(pntr, br, st, this);
        }

    protected virtual void AfterStroke(PlaytimePainter pntr, BrushConfig br, StrokeVector st) {

                pntr.AfterStroke(st);

            if (!br.isSingleBufferBrush() && !br.type(pntr).isA3Dbrush)
                texMGMT.UpdateBufferSegment();

            if ((br.useMask) && (st.mouseUp) && (br.randomMaskOffset))
                br.maskOffset = new Vector2(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));

            foreach (var p in pntr.plugins)
                p.AfterGPUStroke( pntr,  br,  st, this);

        }

}

    public class BrushTypePixel : BrushType {
        static BrushTypePixel _inst;
        public BrushTypePixel() { _inst = this; }
        public static BrushTypePixel inst { get { initIfNull(); return _inst; } }

        protected override string shaderKeyword(bool texcoord2) { return "BRUSH_SQUARE"; }
        public override bool supportedByTex2D { get { return true; } }

        public override string ToString() { return "Pixel"; }

        public override bool isPixelPerfect { get {return true; }}

        public override void Paint(PlaytimePainter pntr, BrushConfig br, StrokeVector st)
        {

            BeforeStroke(pntr, br, st);

            if (st.crossedASeam())
                st.uvFrom = st.uvTo;

            if (texMGMT.BigRT_pair == null) texMGMT.UpdateBuffersState();

            imgData id = pntr.curImgData;

            texMGMT.ShaderPrepareStroke(br, br.speed * 0.05f, id, st.useTexcoord2, st.firstStroke);

        

                rtbrush.localScale = Vector3.one* br.strokeWidth(id.width, false);
                //Vector2 direction = st.delta_uv;
              //  float length = direction.magnitude;
            brushMesh = brushMeshGenerator.inst().GetQuad();//GetLongMesh(length * 256, br.strokeWidth(id.width, false));
            rtbrush.localRotation = Quaternion.identity;//Euler(new Vector3(0, 0, (direction.x > 0 ? -1 : 1) * Vector2.Angle(Vector2.up, direction)));

            rtbrush.localPosition = st.brushWorldPosition;// StrokeVector.brushWorldPositionFrom((st.uvFrom + st.uvTo) / 2);

                texMGMT.Render();

               
            AfterStroke(pntr, br, st);
        }
        
    }

    public class BrushTypeNormal : BrushType {

        static BrushTypeNormal _inst;
        public BrushTypeNormal() { _inst = this; }
        public static BrushTypeNormal inst { get { initIfNull(); return _inst;} }

        protected override string shaderKeyword(bool texcoord) { return "BRUSH_2D"; }

        public override bool supportedByTex2D { get { return true; } }

        public override string ToString ()
	{
		return "Normal";
	}

        public static void Paint (Vector2 uv, BrushConfig br, RenderTexture rt) {

            if (texMGMT.BigRT_pair == null) texMGMT.UpdateBuffersState();

            var id = rt.getImgData();

            texMGMT.ShaderPrepareStroke(br, br.speed * 0.05f, id, false, false);

            float width = br.strokeWidth(id.width, false);

            rtbrush.localScale = Vector3.one;
               
            brushMesh = brushMeshGenerator.inst().GetLongMesh(0, width);
            rtbrush.localRotation = Quaternion.Euler(new Vector3(0, 0, Vector2.Angle(Vector2.up, Vector2.zero)));

            rtbrush.localPosition = StrokeVector.brushWorldPositionFrom(uv);

            texMGMT.Render();

            AfterStroke(br);

          

        }

}

   
    public class BrushTypeDecal : BrushType {

        static BrushTypeDecal _inst;
        public BrushTypeDecal() { _inst = this; }
        public static BrushTypeDecal inst { get { initIfNull(); return _inst; } }
        public override bool supportedBySingleBuffer { get { return false; } }
        protected override string shaderKeyword(bool texcoord) { return "BRUSH_DECAL"; }
	    public override bool isUsingDecals { get { return true; } }

	    public override string ToString ()
	{
		return "Decal";
	}

        Vector2 previousUV;

		public override void Paint (PlaytimePainter pntr, BrushConfig br, StrokeVector st) {

            BeforeStroke(pntr, br, st);

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

				if (texMGMT.BigRT_pair == null) texMGMT.UpdateBuffersState();

				texMGMT.ShaderPrepareStroke(br, 1, id, st.useTexcoord2, st.firstStroke);
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

                texMGMT.Render();

                AfterStroke(pntr, br, st);

            } else
            pntr.AfterStroke (st);
	}

        protected override void AfterStroke(PlaytimePainter pntr, BrushConfig br, StrokeVector st)
        {
            base.AfterStroke(pntr, br, st);

            if (br.decalRotationMethod == DecalRotationMethod.Random)
            {
                br.decalAngle = UnityEngine.Random.Range(-90f, 450f);
                texMGMT.Shader_UpdateDecal(cfg.brushConfig); //pntr.Dec//Update_Brush_Parameters_For_Preview_Shader();
            }
        }

        public override bool PEGI () {
		
		bool brushChanged_RT = false;
		brushChanged_RT |= pegi.select<VolumetricDecal>(ref inspectedBrush.selectedDecal, PainterManager.inst.decals);
		pegi.newLine();

		if (PainterManager.inst.GetDecal(inspectedBrush.selectedDecal) == null)
			pegi.write("Select valid decal; Assign to Painter Camera.");
		pegi.newLine();

        "Continious".toggle("Will keep adding decal every frame while the mouse is down", 80, ref inspectedBrush.decalContinious).nl();

        "Rotation".write("Rotation method", 60);

            inspectedBrush.decalRotationMethod = (DecalRotationMethod)pegi.editEnum<DecalRotationMethod>(inspectedBrush.decalRotationMethod); // "Random Angle", 90);
		pegi.newLine();
		if (inspectedBrush.decalRotationMethod == DecalRotationMethod.Set) {
			"Angle:".write ("Decal rotation", 60);
			brushChanged_RT |= pegi.edit(ref inspectedBrush.decalAngle, -90, 450);
		} else if (inspectedBrush.decalRotationMethod == DecalRotationMethod.StrokeDirection) {
                "Ang Offset:".edit("Angle modifier after the rotation method is applied",80, ref inspectedBrush.decalAngleModifier, -180f, 180f); 
        } 

		pegi.newLine();
		if (!inspectedBrush.mask.GetFlag(BrushMask.A))
			pegi.writeHint("! Alpha chanel is disabled. Decals may not render properly");

		return brushChanged_RT;

	}

}

public class BrushTypeLazy : BrushType {

        static BrushTypeLazy _inst;
        public BrushTypeLazy() { _inst = this; }
        public static BrushTypeLazy inst { get { initIfNull(); return _inst; } }

        public override bool startPaintingTheMomentMouseIsDown { get {return false;}}
	protected override string shaderKeyword(bool texcoord2) {return "BRUSH_2D";}

	public override string ToString () {
		return "Lazy";
	}

	public float LazySpeedDynamic = 1;
	public float LazyAngleSmoothed = 1;
	Vector2 previousDirectionLazy;

	public override void Paint (PlaytimePainter pntr, BrushConfig br, StrokeVector st) {

            BeforeStroke(pntr, br, st);
            //	Vector2 outb = new Vector2(Mathf.Floor(st.uvTo.x), Mathf.Floor(st.uvTo.y));
            //	st.uvTo -= outb;
            //	st.uvFrom -= outb;

            Vector2 delta_uv = st.delta_uv;//uv - st.uvFrom;//.Previous_uv;
		float magn = delta_uv.magnitude;

		float width = br.Size(false) / ((float)pntr.curImgData.width) * 4;
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
				PainterManager r = texMGMT;
			//RenderTexturePainter.inst.RenderLazyBrush(painter.Previous_uv, uv, brush.speed * 0.05f, painter.curImgData, brush, painter.LmouseUP, smooth );
				if (texMGMT.BigRT_pair == null) texMGMT.UpdateBuffersState();

				imgData id = pntr.curImgData;

				float meshWidth = br.strokeWidth(id.width, false); //.Size(false) / ((float)id.width) * 2 * rtp.orthoSize;

				Transform tf = rtbrush;

				Vector2 direction = st.delta_uv;//uvTo - uvFrom;

				bool isTail = st.firstStroke;//(!previousTo.Equals(uvFrom));

				if ((!isTail) && (!smooth)) {
                    //Debug.Log ("Junction point");
                    r.ShaderPrepareStroke(br, br.speed * 0.05f, id, false, false);

                    Vector3 junkPoint = st.uvFrom + st.previousDelta * 0.01f;
					brushMesh = brushMeshGenerator.inst().GetStreak(uvToPosition(st.uvFrom), uvToPosition(junkPoint), meshWidth, true, false);
					tf.localScale = Vector3.one;
					tf.localRotation = Quaternion.identity;
					tf.localPosition = new Vector3(0, 0, 10);


                    r.Render();//Render_UpdateSecondBufferIfUsing(id);
					st.uvFrom = junkPoint;
					isTail = true;
				}

                r.ShaderPrepareStroke(br, br.speed * 0.05f, id, false, st.firstStroke);

                brushMesh = brushMeshGenerator.inst().GetStreak(uvToPosition(st.uvFrom), uvToPosition(st.uvTo), meshWidth, st.mouseUp, isTail);
                tf.localScale = Vector3.one;
				tf.localRotation = Quaternion.identity;
				tf.localPosition = new Vector3(0, 0, 10);

				st.previousDelta = direction;

                r.Render();

                AfterStroke(pntr,br,st);

		}
	}
}

    public class BrushTypeSphere : BrushType {

        static BrushTypeSphere _inst;
        public BrushTypeSphere() { _inst = this; }
        public static BrushTypeSphere inst { get { initIfNull(); return _inst; } }

        protected override string shaderKeyword(bool texcoord2) { return  (texcoord2 ? "BRUSH_3D_TEXCOORD2" : "BRUSH_3D"); }

        public override bool isA3Dbrush { get { return true; } }
        public override bool supportedForTerrain_RT { get { return false; } }
        public override bool useGrid   {  get { return cfg.useGridForBrush; } }

        public override string ToString () {
		return "Sphere";
	}

        static void PrepareSphereBrush(imgData id, BrushConfig br, StrokeVector st) {
            if (texMGMT.BigRT_pair == null) texMGMT.UpdateBuffersState();

            if (st.mouseDwn)
                st.posFrom = st.posTo;

            texMGMT.ShaderPrepareStroke(br, br.speed * 0.05f, id, st.useTexcoord2, st.firstStroke);

            Vector2 offset = id.offset - st.unRepeatedUV.Floor();
            
            st.SetWorldPosInShader();

            Shader.SetGlobalVector(PainterConfig.BRUSH_EDITED_UV_OFFSET, new Vector4(id.tiling.x, id.tiling.y, offset.x, offset.y));
            Shader.SetGlobalVector(PainterConfig.BRUSH_ATLAS_SECTION_AND_ROWS, new Vector4(0, 0, 1, 0));
        }

        public override void Paint(PlaytimePainter pntr, BrushConfig br, StrokeVector st)  {

            imgData id = pntr.curImgData;

            BeforeStroke(pntr, br, st);

            PrepareSphereBrush(id, br, st);

            if (!st.mouseDwn) {
                texMGMT.brushRendy.UseMeshAsBrush(pntr);
                texMGMT.Render();  
            }

            AfterStroke(pntr, br, st);

          
        }

        public static void Paint(RenderTexture rt, GameObject go, SkinnedMeshRenderer skinner, BrushConfig br, StrokeVector st, int submeshIndex) {
            br.blitMode.PrePaint(null, br, st);
            PrepareSphereBrush(rt.getImgData(), br, st);
            texMGMT.brushRendy.UseSkinMeshAsBrush(go, skinner, submeshIndex);
            texMGMT.Render();
            AfterStroke(br);
        }

        public static void Paint(RenderTexture rt, GameObject go, Mesh mesh, BrushConfig br, StrokeVector st,  List<int> submeshIndex) {
            br.blitMode.PrePaint(null, br, st);

            PrepareSphereBrush(rt.getImgData(), br, st);
            texMGMT.brushRendy.UseMeshAsBrush(go, mesh, submeshIndex);
            texMGMT.Render();
            AfterStroke(br);
        }

        public static void PaintAtlased(RenderTexture rt, GameObject go, Mesh mesh, BrushConfig br, StrokeVector st, List<int> submeshIndex, int A_Textures_in_row)
        {

            br.blitMode.PrePaint(null, br, st);

            Shader.SetGlobalVector(PainterConfig.BRUSH_ATLAS_SECTION_AND_ROWS, new Vector4(0, 0, A_Textures_in_row, 1));

            PrepareSphereBrush(rt.getImgData(), br, st);
            texMGMT.brushRendy.UseMeshAsBrush(go, mesh, submeshIndex);
            texMGMT.Render();
            AfterStroke(br);

            Shader.SetGlobalVector(PainterConfig.BRUSH_ATLAS_SECTION_AND_ROWS, new Vector4(0, 0, 1, 0));
        }

        public override bool PEGI() {
            bool changed = base.PEGI();

            changed |= "Use Grid".toggle(60 ,ref cfg.useGridForBrush);

            return changed;
        }
    }
}