using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace Playtime_Painter.Examples { 


    public class PaintingCollision {
		public StrokeVector vector;
		public PlaytimePainter painter;

		public PaintingCollision (PlaytimePainter p){
			painter = p;
			vector = new StrokeVector ();
		}
	}

    [ExecuteInEditMode]
    public class PainterBall : MonoBehaviour  , IPEGI

    {

        public MeshRenderer rendy;
        public Rigidbody rigid;
        public SphereCollider _collider;

		public List<PaintingCollision> paintingOn = new List<PaintingCollision>();
        public BrushConfig brush = new BrushConfig();

        PaintingCollision TryAddPainterFrom( GameObject go) {
            PlaytimePainter target = go.GetComponent<PlaytimePainter>();

            if (target && !target.LockTextureEditing)   {
                PaintingCollision col = new PaintingCollision(target);
                paintingOn.Add(col);
                col.vector.posFrom = transform.position;
                col.vector.firstStroke = true;
                target.UpdateOrSetTexTarget(TexTarget.RenderTexture);

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
            foreach (PaintingCollision p in paintingOn)
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
            brush.TypeSet(false, BrushTypeSphere.Inst);
            if (!rendy) 
                rendy = GetComponent<MeshRenderer>();
            if (!rigid)
                rigid = GetComponent<Rigidbody>();
            if (!_collider)
                _collider = GetComponent<SphereCollider>();

            rendy.sharedMaterial.color = brush.Color;
            brush.TargetIsTex2D = false;
        }

        private void Update() {

            brush.brush3DRadius = transform.lossyScale.x*0.7f;

			foreach (PaintingCollision col in paintingOn){
				PlaytimePainter p = col.painter;
				if (brush.IsA3dBrush(p)) {
                    StrokeVector v = col.vector;
                    v.posTo = transform.position;
                    brush.Paint(v, p);
                  
				}

            }
        }

#if PEGI
        public bool Inspect() {
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
                if ((brush.TargetIsTex2D) || (!brush.IsA3dBrush(null))) {
                    brush.TargetIsTex2D = false;
                    brush.TypeSet(false,  BrushTypeSphere.Inst);

                    pegi.resetOneTimeHint("PaintBall_brushHint");
                }
            }

            if (brush.ColorSliders()) 
                rendy.sharedMaterial.color = brush.Color;

            return false;
        }
#endif
    }
}
