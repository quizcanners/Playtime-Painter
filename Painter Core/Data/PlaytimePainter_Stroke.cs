using QuizCanners.Migration;
using QuizCanners.Utils;
using UnityEngine;

namespace PainterTool {
    
    public class Stroke : ICfg {

	    public Vector2 uvFrom;
	    public Vector3 posFrom;
	    public Vector2 uvTo;
	    public Vector3 posTo;
        public Vector3 collisionNormal;
        public Vector2 unRepeatedUv;
        
        public Vector2 previousDelta;
	    public float avgBrushSpeed;

        public bool UV_Set;

        private bool _mouseDownEvent;
        private bool _mouseUpEvent;
        
        public bool firstStroke; // For cases like Lazy Brush, when painting doesn't start on the first frame.
        public bool mouseHeld;
        public static bool pausePlayback;

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
        
        public override string ToString() => "Pos: {0} UV: {1}".F(DeltaWorldPos, DeltaUv); 

	    public Vector2 DeltaUv => uvTo - uvFrom;
        public Vector3 DeltaWorldPos => posTo - posFrom;

        public void Paint(PainterComponent painter, Brush brush) => brush.Paint(painter.Command); //this, painter);
        
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



        public CfgEncoder Encode() {

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

        public void DecodeTag(string key, CfgData data) {

            switch (key) {
                case "fU": Down_Internal(data.ToVector2());  break;
                case "fP": Down_Internal(data.ToVector3());  break;
                case "tU": uvTo = data.ToVector2(); break;
                case "tP": posTo = data.ToVector3(); break;
                case "Up": MouseUpEvent = true; break;
            }

        }
        #endregion

        public void OnStrokeEnd() {

            MouseUpEvent = mouseHeld;

            MouseDownEvent = false;
            mouseHeld = false;
        }

        public void OnStrokeStart(Vector2 pos)
        {
            UV_Set = true;

            if (!mouseHeld)
                Down_Internal(pos);
            else
            {
                uvFrom = uvTo;
                mouseHeld = true;
                MouseDownEvent = false;
            }

            uvTo = pos;
        }

        public void OnStrokeStart(Vector3 pos) 
        {
            if (!mouseHeld) 
                Down_Internal(pos);
            else {
                posFrom = posTo;
                mouseHeld = true;
                MouseDownEvent = false;
            }

            posTo = pos;
        }

        public void OnStrokeContiniousStart(RaycastHit hit, bool texcoord2)
        {
            var pos = hit.point;
            var uv = (texcoord2 ? hit.textureCoord2 : hit.textureCoord).To01Space();
            
            if (!mouseHeld)
                Down_Internal(pos, uv);
            else {
                posFrom = posTo;
                uvFrom = uvTo;
                mouseHeld = true;
                MouseDownEvent = false;
            }

            posTo = pos;
            uvTo = uv;
        }

        private void Down_Internal(Vector3 pos, Vector2 uv) {
            On_Down_Internal();
            uvFrom = uv;
            UV_Set = true;
            posFrom = pos;
        }

        private void Down_Internal( Vector3 pos) { 
            On_Down_Internal();
            posFrom = pos;
        }

        private void Down_Internal( Vector2 uv) {
            On_Down_Internal();
            uvFrom  = uv;
            UV_Set = true;

        }

        private void On_Down_Internal() {

            firstStroke = true;
            MouseDownEvent = true;
            mouseHeld = true;
        }

        internal void FeedWorldPosInShader()
        {
            PainterShaderVariables.BRUSH_WORLD_POS_FROM.GlobalValue = new Vector4(posFrom.x, posFrom.y, posFrom.z, 0);
            PainterShaderVariables.BRUSH_WORLD_POS_TO.GlobalValue = new Vector4(posTo.x, posTo.y, posTo.z, DeltaWorldPos.magnitude);
        }

        internal static Vector3 GetCameraProjectionTarget(Vector2 uv) => ((uv * 2 - Vector2.one) * Singleton_PainterCamera.OrthographicSize).ToVector3(10);

        public  Vector3 CameraProjectionTarget => GetCameraProjectionTarget(uvTo);

        private readonly Gate.Frame _frameGate = new(Gate.InitialValue.StartArmed);

        public void TrySetPreviousValues() 
        {
            if (_frameGate.TryEnter())
            {
                previousDelta = MouseDownEvent ? Vector2.zero : (uvTo - uvFrom);
                uvFrom = uvTo;
                posFrom = posTo;
            }
        }


        public void From(RaycastHit hit, bool useTexcoord2)
        {
            if (hit.collider.GetType() == typeof(MeshCollider))
                UV_Set = true;

            uvFrom = uvTo = (useTexcoord2 ? hit.textureCoord2 : hit.textureCoord).To01Space();
            posFrom = posTo = hit.point;
        }

        public Stroke()
        {
            On_Down_Internal();
        }

        public Stroke (ContactPoint point) 
        {
            posFrom = posTo = point.point;
        }

        public Stroke(Vector3 from, Vector3 strokeVector)
        {
            posFrom = posTo = from;
            posTo += strokeVector;
        }

        public Stroke(ContactPoint point, Vector3 strokeVector)
        {
            posFrom = posTo = point.point;
            posTo += strokeVector;
        }

        public Stroke (RaycastHit hit, bool texcoord2) {
            From(hit, texcoord2);
            On_Down_Internal();
        }


        public Stroke(RaycastHit hit, Texture tex)
        {
            if (!tex)
                Debug.LogError("Texture used to create Stroke is Null");

            bool texcoord2 = tex ? tex.GetTextureMeta()[TextureCfgFlags.Texcoord2] : false;

            From(hit, texcoord2);
            On_Down_Internal();
        }


        public Stroke(Vector3 pos)
        {
            posFrom = posTo = pos;
            On_Down_Internal();
        }

        public Stroke(Vector2 uv)
        {
            uvFrom = uvTo = uv;
            On_Down_Internal();
        }

        public Stroke(Stroke  other)
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
            MouseUpEvent = other.MouseUpEvent;

            On_Down_Internal();
        }
    }

}