using UnityEngine;
using System;
using System.Runtime.CompilerServices;
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

        private bool _mouseDownEvent;
        private bool _mouseUpEvent;

        public bool MouseDownEvent
        {
            get { return _mouseDownEvent; }
            set
            {
                _mouseDownEvent = value;
                if (value)
                    _mouseUpEvent = false;
            }
        }
	    public bool firstStroke; // For cases like Lazy Brush, when painting doesn't start on the first frame.

        public bool MouseUpEvent
        {
            get { return _mouseUpEvent; }
            set
            {
                _mouseUpEvent = value;
                if (value)
                    _mouseDownEvent = false;
            }
        }
        public bool mouseHeld;

        public static bool pausePlayback;

        public override string ToString() => "Pos: {0} UV: {1}".F(DeltaWorldPos, DeltaUv); 

	    public Vector2 DeltaUv => uvTo - uvFrom;
        public Vector3 DeltaWorldPos => posTo - posFrom;

        public PlaytimePainter Paint(PlaytimePainter painter, BrushConfig brush) => brush.Paint(this, painter);
        

        public bool CrossedASeam() {

            if (MouseDownEvent)
                return false;

            var newDelta = uvTo - uvFrom;

            var prevMagnitude = previousDelta.magnitude;

            return ((Vector2.Dot(previousDelta.normalized, newDelta.normalized) < 0)
                        && (newDelta.magnitude > 0.1) && (newDelta.magnitude > prevMagnitude * 4))
                        || (newDelta.magnitude > 0.2f)  ;

            }

        #region Encode & Decode
        public override CfgEncoder Encode() {

            var s = new CfgEncoder();

            if (MouseDownEvent) s.Add("fU", uvFrom, 4);
            if (MouseDownEvent) s.Add("fP", posFrom, 4);

            s.Add("tU", uvTo, 4);
            s.Add("tP", posTo, 4);

            if (MouseUpEvent)
                s.Add_String("Up", "_");

            return s;
        }
        
        public CfgEncoder Encode(bool worldSpace) {

            var s = new CfgEncoder();

            if (MouseDownEvent) {
                if (worldSpace)
                    s.Add("fP", posFrom, 4);
                else
                    s.Add("fU", uvFrom, 4);
            }

            if (worldSpace)
                s.Add("tP", posTo, 4);
            else
                s.Add("tU", uvTo, 4);

            if (MouseUpEvent)
                s.Add_String("Up", "_");

            return s;
        }

        public override bool Decode(string tg, string data) {

            switch (tg) {
                case "fU": Down(data.ToVector2());  break;
                case "fP": Down(data.ToVector3());  break;
                case "tU": uvTo = data.ToVector2(); break;
                case "tP": posTo = data.ToVector3(); break;
                case "Up": MouseUpEvent = true; break;
                default: return false;
            }
            return true;

        }
        #endregion

        public void OnMouseUnPressed() {

            MouseUpEvent = mouseHeld;

            MouseDownEvent = false;
            mouseHeld = false;
        }

        public void OnMousePressed(Vector2 pos)
        {
            if (!mouseHeld)
                Down(pos);
            else
            {
                uvFrom = uvTo;
                mouseHeld = true;
                MouseDownEvent = false;
            }

            uvTo = pos;
        }

        public void OnMousePressed(Vector3 pos) {
            if (!mouseHeld) 
                Down(pos);
            else {
                posFrom = posTo;
                mouseHeld = true;
                MouseDownEvent = false;
            }

            posTo = pos;
        }

        public void OnMousePressed(RaycastHit hit, bool texcoord2)
        {

            var pos = hit.point;
            var uv = (texcoord2 ? hit.textureCoord2 : hit.textureCoord).To01Space();
            
            if (!mouseHeld)
                Down(pos, uv);
            else {
                posFrom = posTo;
                uvFrom = uvTo;
                mouseHeld = true;
                MouseDownEvent = false;
            }

            posTo = pos;
            uvTo = uv;
        }

        private void Down(RaycastHit hit, bool texcoord2) =>
            Down(hit.point, (texcoord2 ? hit.textureCoord2 : hit.textureCoord).To01Space());
        
        private void Down(Vector3 pos, Vector2 uv) {
            Down_Internal();
            uvFrom = uv;
            posFrom = pos;
        }

        private void Down( Vector3 pos) { 
            Down_Internal();
            posFrom = pos;
        }

        private void Down( Vector2 uv) {
            Down_Internal();
            uvFrom  = uv;
        }

        private void Down_Internal() {

            firstStroke = true;
            MouseDownEvent = true;
            mouseHeld = true;
        }

        public void SetWorldPosInShader()
        {
            PainterShaderVariables.BRUSH_WORLD_POS_FROM.GlobalValue = new Vector4(posFrom.x, posFrom.y, posFrom.z, 0);
            PainterShaderVariables.BRUSH_WORLD_POS_TO.GlobalValue = new Vector4(posTo.x, posTo.y, posTo.z, DeltaWorldPos.magnitude);
        }

        public static Vector3 BrushWorldPositionFrom(Vector2 uv) => ((uv * 2 - Vector2.one) * PainterCamera.OrthographicSize).ToVector3(10);

        public  Vector3 BrushWorldPosition => BrushWorldPositionFrom(uvTo);
        
        public void SetPreviousValues() {
            previousDelta = MouseDownEvent ? Vector2.zero : (uvTo - uvFrom);
		    uvFrom = uvTo;
            posFrom = posTo;
        }

        public StrokeVector()
        {
            Down_Internal();
        }

        public StrokeVector (RaycastHit hit, bool texcoord2) {
            uvFrom = uvTo = (texcoord2 ? hit.textureCoord2 : hit.textureCoord).To01Space();
            posFrom = posTo = hit.point;
            Down_Internal();
        }



        public StrokeVector(Vector3 pos)
        {
            posFrom = posTo = pos;
            Down_Internal();
        }

        public StrokeVector(Vector2 uv)
        {
            uvFrom = uvTo = uv;
            Down_Internal();
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
            
            MouseDownEvent = other.MouseDownEvent;
            firstStroke = other.firstStroke ; // For cases like Lazy Brush, when painting doesn't start on the first frame.
            MouseUpEvent = other.MouseDownEvent;

            Down_Internal();
        }
    }

    public class BrushStrokePainterImage
    {
        public StrokeVector stroke;
        public TextureMeta image;
        public BrushConfig brush;
        public PlaytimePainter painter;

        public BrushStrokePainterImage(StrokeVector s, TextureMeta id, BrushConfig br, PlaytimePainter pp)
        {
            stroke = s;
            image = id;
            brush = br;
            painter = pp;
        }
    }


}