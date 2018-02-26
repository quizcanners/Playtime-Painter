using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using StoryTriggerData;

namespace Painter {

[Serializable]
    public class StrokeVector : abstract_STD {

	public Vector2 uvFrom;
	public Vector3 posFrom;
	public Vector2 uvTo;
	public Vector3 posTo;
    public bool texcoord2; // For Sphere Brush
    public Vector2 previousDelta;
	public float avgBrushSpeed;
     

	public bool mouseDwn;
	public bool firstStroke; // For cases like Lazy Brush, when painting doesn't start on the first frame.
	public bool mouseUp;

        public static bool PausePlayback;


	public Vector2 delta_uv { get { return uvTo - uvFrom; } }
	public Vector3 delta_WorldPos { get { return posTo - posFrom; } }

    public bool crossedASeam() {

            if (mouseDwn)
                return false;

            Vector2 newDelta = uvTo - uvFrom;

            return (((Vector2.Dot(previousDelta.normalized, newDelta.normalized) < 0)
                     && (newDelta.magnitude > 0.1) && (newDelta.magnitude > previousDelta.magnitude * 4)) || (newDelta.magnitude > 0.2f));

        }


    public override stdEncoder Encode() {

        stdEncoder s = new stdEncoder();

        if (mouseDwn) s.Add("fU", uvFrom, 4);
        if (mouseDwn) s.Add("fP", posFrom, 4);

        s.Add("tU", uvTo, 4);
        s.Add("tP", posTo, 4);

        if (mouseUp)
            s.AddText("Up", "_");

        return s;
    }


        public stdEncoder Encode(bool worldSpace) {

            stdEncoder s = new stdEncoder();

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
                s.AddText("Up", "_");

            return s;
        }

        public override void Decode(string tag, string data) {

            switch (tag) {
                case "fU": dwn(data.ToVector2());  break;
                case "fP": dwn(data.ToVector3());  break;
                case "tU": uvTo = data.ToVector2(); break;
                case "tP": posTo = data.ToVector3(); break;
                case "Up": mouseUp = true; break;
            }

        }

        public void dwn( Vector3 pos) {

            dwn();
            posFrom = pos;
        }

        public void dwn(Vector2 uv) {
          //  "decoding down".Log();
            dwn();
            uvFrom  = uv;
        }

        public void dwn(Vector2 uv, Vector3 pos) {

            dwn();
            uvFrom = uv;
            posFrom = pos;
        }

        void dwn() {
            firstStroke = true;
            mouseDwn = true;
            mouseUp = false;
            
        }

        public const string storyTag = "s";

        public override string getDefaultTagName() {
            return storyTag;   
        }

        public void SetWorldPosInShader()
        {
            Shader.SetGlobalVector(PainterConfig.BRUSH_WORLD_POS_FROM, new Vector4(posFrom.x, posFrom.y, posFrom.z, 0));
            Shader.SetGlobalVector(PainterConfig.BRUSH_WORLD_POS_TO, new Vector4(posTo.x, posTo.y, posTo.z, delta_WorldPos.magnitude));
        }

	public static Vector3 brushWorldPositionFrom (Vector2 uv) {  
				Vector2 v2 = ((uv)*2 - Vector2.one) * PainterManager.orthoSize;

				return new Vector3 (v2.x, v2.y, 10);
    }

      public  Vector3 brushWorldPosition
        {
            get
            {
                return brushWorldPositionFrom(uvTo);
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

        }

        public StrokeVector (RaycastHit hit) {
            uvFrom = uvTo = hit.textureCoord.To01Space();
            posFrom = posTo = hit.point;
        }
        public StrokeVector(Vector3 pos)
        {

            posFrom = posTo = pos;
        }

    }

}