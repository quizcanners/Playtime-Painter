using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace Playtime_Painter {

#if UNITY_EDITOR

    using UnityEditor;

   
    [CustomEditor(typeof(PainterBall))]
    public class PainterBallEditor : Editor    {

        public override void OnInspectorGUI() {
            ef.start(serializedObject);
            ((PainterBall)target).PEGI();
            ef.end();
        }
    }
#endif


    public class paintingCollision {
		public StrokeVector vector;
		public PlaytimePainter painter;

		public paintingCollision (PlaytimePainter p){
			painter = p;
			vector = new StrokeVector ();
		}
	}

    [ExecuteInEditMode]
    public class PainterBall : MonoBehaviour {

        public MeshRenderer rendy;
        public Rigidbody rigid;
        public SphereCollider _collider;

		public List<paintingCollision> paintingOn = new List<paintingCollision>();
        public BrushConfig brush = new BrushConfig();

        paintingCollision TryAddPainterFrom( GameObject go) {
            PlaytimePainter target = go.GetComponent<PlaytimePainter>();

            if (target != null && !target.LockTextureEditing)   {
                paintingCollision col = new paintingCollision(target);
                paintingOn.Add(col);
                col.vector.posFrom = transform.position;
                col.vector.firstStroke = true;
                target.UpdateOrSetTexTarget(texTarget.RenderTexture);

                return col;
            }

            return null;
        }

        public void OnCollisionEnter(Collision collision) {
            //var pcol = 
                TryAddPainterFrom(collision.gameObject);
          /*  if (pcol != null) {

                    var cp = collision.contacts[0];
                    RaycastHit hit;
                    Ray ray = new Ray(cp.point - cp.normal * 0.05f, cp.normal);
                    if (cp.otherCollider.Raycast(ray, out hit, 0.1f))
                        pcol.vector.uvFrom = hit.textureCoord;
                    
                
            }*/

        }



        public void OnTriggerEnter(Collider collider) {
            TryAddPainterFrom(collider.gameObject);
         
        }


        void TryRemove(GameObject go)
        {
            foreach (paintingCollision p in paintingOn)
                if (p.painter.gameObject == go)
                {
                    paintingOn.Remove(p);
                    return;
                }
        }

        public void OnTriggerExit(Collider collider) {

            TryRemove(collider.gameObject);

        }

        public void OnCollisionExit(Collision collision)
        {
            TryRemove(collision.gameObject);
        }

        public void OnEnable()  {
            brush.typeSet(false, BrushTypeSphere.inst);
            if (rendy == null) 
                rendy = GetComponent<MeshRenderer>();
            if (rigid == null)
                rigid = GetComponent<Rigidbody>();
            if (_collider == null)
                _collider = GetComponent<SphereCollider>();

            rendy.sharedMaterial.color = brush.colorLinear.ToGamma();
            brush.TargetIsTex2D = false;
        }

        private void Update() {

            brush.Brush3D_Radius = transform.lossyScale.x*0.7f;

			foreach (paintingCollision col in paintingOn){
				PlaytimePainter p = col.painter;
				if (brush.IsA3Dbrush(p)) {
                    StrokeVector v = col.vector;
                    v.posTo = transform.position;
                    brush.Paint(v, p);
                  
				}

            }
        }


        public void PEGI() {
            ("Painting on " + paintingOn.Count + " objects").nl();

            if ((_collider.isTrigger) && ("Make phisical".Click().nl()))
            {
                _collider.isTrigger = false;
                rigid.isKinematic = false;
                rigid.useGravity = true;
            }

            if ((!_collider.isTrigger) && ("Make Trigger".Click().nl()))
            {
                _collider.isTrigger = true;
                rigid.isKinematic = true;
                rigid.useGravity = false;
            }



            float size = transform.localScale.x;
            if ("Size:".edit("Size of the ball", 50, ref size, 0.1f, 10).nl())
                transform.localScale = Vector3.one * size;

          

            pegi.writeOneTimeHint("Painter ball made for World Space Brushes only", "PaintBall_brushHint");
          
            if  ((brush.Targets_PEGI().nl()) || (brush.Mode_Type_PEGI().nl())) {
                if ((brush.TargetIsTex2D) || (!brush.IsA3Dbrush(null))) {
                    brush.TargetIsTex2D = false;
                    brush.typeSet(false,  BrushTypeSphere.inst);

                    pegi.resetOneTimeHint("PaintBall_brushHint");
                }
            }

            if (brush.ColorSliders_PEGI()) 
                rendy.sharedMaterial.color = brush.colorLinear.ToGamma();
        }

    }
}
