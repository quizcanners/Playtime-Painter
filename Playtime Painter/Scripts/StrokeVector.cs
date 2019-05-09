using UnityEngine;
using System;
using QuizCannersUtilities;

namespace PlaytimePainter {
    
    public class StrokeVector : AbstractCfg {

	    public Vector2 uvFrom;
	    public Vector3 posFrom;
	    public Vector2 uvTo;
	    public Vector3 posTo;
        public Vector3 collisionNormal;
        public Vector2 unRepeatedUv;
        
        public Vector2 previousDelta;
	    public float avgBrushSpeed;
     
	    public bool mouseDwn;
	    public bool firstStroke; // For cases like Lazy Brush, when painting doesn't start on the first frame.
	    public bool mouseUp;

        public static bool pausePlayback;


	    public Vector2 DeltaUv => uvTo - uvFrom;
        public Vector3 DeltaWorldPos => posTo - posFrom;

        public PlaytimePainter Paint(PlaytimePainter painter, BrushConfig brush) => brush.Paint(this, painter);
        

        public bool CrossedASeam() {

            if (mouseDwn)
                return false;

            var newDelta = uvTo - uvFrom;

            var prevMagnitude = previousDelta.magnitude;

            return ((Vector2.Dot(previousDelta.normalized, newDelta.normalized) < 0)
                        && (newDelta.magnitude > 0.1) && (newDelta.magnitude > prevMagnitude * 4))
                        || (newDelta.magnitude > 0.2f)  ;

            }

        public override CfgEncoder Encode() {

            var s = new CfgEncoder();

            if (mouseDwn) s.Add("fU", uvFrom, 4);
            if (mouseDwn) s.Add("fP", posFrom, 4);

            s.Add("tU", uvTo, 4);
            s.Add("tP", posTo, 4);

            if (mouseUp)
                s.Add_String("Up", "_");

            return s;
        }
        
        public CfgEncoder Encode(bool worldSpace) {

            var s = new CfgEncoder();

            if (mouseDwn) {
                if (worldSpace)
                    s.Add("fP", posFrom, 4);
                else
                    s.Add("fU", uvFrom, 4);
            }

            if (worldSpace)
                s.Add("tP", posTo, 4);
            else
                s.Add("tU", uvTo, 4);

            if (mouseUp)
                s.Add_String("Up", "_");

            return s;
        }

        public override bool Decode(string tg, string data) {

            switch (tg) {
                case "fU": Dwn(data.ToVector2());  break;
                case "fP": Dwn(data.ToVector3());  break;
                case "tU": uvTo = data.ToVector2(); break;
                case "tP": posTo = data.ToVector3(); break;
                case "Up": mouseUp = true; break;
                default: return false;
            }
            return true;

        }

        private void Dwn( Vector3 pos) {

            Dwn();
            posFrom = pos;
        }

        private void Dwn(Vector2 uv) {
            Dwn();
            uvFrom  = uv;
        }

        private void Dwn() {
            firstStroke = true;
            mouseDwn = true;
            mouseUp = false;
            
        }

        public void SetWorldPosInShader()
        {
           PainterDataAndConfig.BRUSH_WORLD_POS_FROM.GlobalValue = new Vector4(posFrom.x, posFrom.y, posFrom.z, 0);
            PainterDataAndConfig.BRUSH_WORLD_POS_TO.GlobalValue = new Vector4(posTo.x, posTo.y, posTo.z, DeltaWorldPos.magnitude);
        }

        public static Vector3 BrushWorldPositionFrom(Vector2 uv) => ((uv * 2 - Vector2.one) * PainterCamera.OrthographicSize).ToVector3(10);

        public  Vector3 BrushWorldPosition => BrushWorldPositionFrom(uvTo);
        
        public void SetPreviousValues() {
            previousDelta = mouseDwn ? Vector2.zero : (uvTo - uvFrom);
		    uvFrom = uvTo;
            posFrom = posTo;
        }

        public StrokeVector()
        {
            Dwn();
        }

        public StrokeVector (RaycastHit hit, bool texcoord2) {
            uvFrom = uvTo = (texcoord2 ? hit.textureCoord : hit.textureCoord2).To01Space();
            posFrom = posTo = hit.point;
            Dwn();
        }

        public StrokeVector(Vector3 pos)
        {
            posFrom = posTo = pos;
            Dwn();
        }

        public StrokeVector(Vector2 uv)
        {
            uvFrom = uvTo = uv;
            Dwn();
        }

        public StrokeVector(StrokeVector  other)
        {
            uvFrom = other.uvFrom;
            posFrom = other.posFrom;
            uvTo = other.uvTo;

            posTo = other.posTo;
            unRepeatedUv = other.unRepeatedUv;
            
            previousDelta = other.previousDelta;
            avgBrushSpeed = other.avgBrushSpeed;
            
            mouseDwn = other.mouseDwn;
            firstStroke = other.firstStroke ; // For cases like Lazy Brush, when painting doesn't start on the first frame.
            mouseUp = other.mouseDwn;

            Dwn();
        }
    }

    public class BrushStrokePainterImage
    {
        public StrokeVector stroke;
        public ImageMeta image;
        public BrushConfig brush;
        public PlaytimePainter painter;

        public BrushStrokePainterImage(StrokeVector s, ImageMeta id, BrushConfig br, PlaytimePainter pp)
        {
            stroke = s;
            image = id;
            brush = br;
            painter = pp;
        }
    }


}