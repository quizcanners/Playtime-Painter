using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SharedTools_Stuff;
using PlayerAndEditorGUI;

namespace Playtime_Painter {

[Serializable]
    public class StrokeVector : Abstract_STD {

	public Vector2 uvFrom;
	public Vector3 posFrom;
	public Vector2 uvTo;
	public Vector3 posTo;
       // public Vector2 inMeshUV;
        public Vector2 unRepeatedUV;

     //public bool useTexcoord2; // For Sphere Brush
    public Vector2 previousDelta;
	public float avgBrushSpeed;
     

	public bool mouseDwn;
	public bool firstStroke; // For cases like Lazy Brush, when painting doesn't start on the first frame.
	public bool mouseUp;

        public static bool PausePlayback;


	public Vector2 Delta_uv { get { return uvTo - uvFrom; } }
	public Vector3 Delta_WorldPos { get { return posTo - posFrom; } }

    public PlaytimePainter Paint(PlaytimePainter painter, BrushConfig brush) {
            return brush.Paint(this, painter);
    }

    public bool CrossedASeam() {

            if (mouseDwn)
                return false;

            Vector2 newDelta = uvTo - uvFrom;

            float prevMagn = previousDelta.magnitude;

            return ((Vector2.Dot(previousDelta.normalized, newDelta.normalized) < 0)
                     && (newDelta.magnitude > 0.1) && (newDelta.magnitude > prevMagn * 4))
                     || (newDelta.magnitude > 0.2f) 
                     //|| (newDelta.magnitude > prevMagn * 10)
                     
                     ;

        }

    public override StdEncoder Encode() {

        StdEncoder s = new StdEncoder();

        if (mouseDwn) s.Add("fU", uvFrom, 4);
        if (mouseDwn) s.Add("fP", posFrom, 4);

        s.Add("tU", uvTo, 4);
        s.Add("tP", posTo, 4);

        if (mouseUp)
            s.Add_String("Up", "_");

        return s;
    }


        public StdEncoder Encode(bool worldSpace) {

            StdEncoder s = new StdEncoder();

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

        public override bool Decode(string tag, string data) {

            switch (tag) {
                case "fU": Dwn(data.ToVector2());  break;
                case "fP": Dwn(data.ToVector3());  break;
                case "tU": uvTo = data.ToVector2(); break;
                case "tP": posTo = data.ToVector3(); break;
                case "Up": mouseUp = true; break;
                default: return false;
            }
            return true;

        }

        public void Dwn( Vector3 pos) {

            Dwn();
            posFrom = pos;
        }

        public void Dwn(Vector2 uv) {
          //  "decoding down".Log();
            Dwn();
            uvFrom  = uv;
        }

        public void Dwn(Vector2 uv, Vector3 pos) {

            Dwn();
            uvFrom = uv;
            posFrom = pos;
        }

        void Dwn() {
            firstStroke = true;
            mouseDwn = true;
            mouseUp = false;
            
        }

      //  public const string storyTag = "s";

        public void SetWorldPosInShader()
        {
            Shader.SetGlobalVector(PainterDataAndConfig.BRUSH_WORLD_POS_FROM, new Vector4(posFrom.x, posFrom.y, posFrom.z, 0));
            Shader.SetGlobalVector(PainterDataAndConfig.BRUSH_WORLD_POS_TO, new Vector4(posTo.x, posTo.y, posTo.z, Delta_WorldPos.magnitude));
        }

	public static Vector3 BrushWorldPositionFrom (Vector2 uv) {  
				Vector2 v2 = ((uv)*2 - Vector2.one) * PainterCamera.orthoSize;

				return new Vector3 (v2.x, v2.y, 10);
    }

      public  Vector3 BrushWorldPosition
        {
            get
            {
                return BrushWorldPositionFrom(uvTo);
            }
      }


    public void SetPreviousValues() {
           
             
        previousDelta = mouseDwn ? Vector2.zero : (uvTo - uvFrom);
		uvFrom = uvTo;
        posFrom = posTo;

          //  Debug.Log(" prev From " + uvFrom + " to " + uvTo);
        }

        public StrokeVector()
        {
            Dwn();
        }

        public StrokeVector (RaycastHit hit, bool texcoord2) {
            //inMeshUV = hit.textureCoord;
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
            unRepeatedUV = other.unRepeatedUV;
            
            previousDelta = other.previousDelta;
            avgBrushSpeed = other.avgBrushSpeed;
            
            mouseDwn = other.mouseDwn;
            firstStroke = other.firstStroke ; // For cases like Lazy Brush, when painting doesn't start on the first frame.
            mouseUp = other.mouseDwn;

            Dwn();
        }
    }

}