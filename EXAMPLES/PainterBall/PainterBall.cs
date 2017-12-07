using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;

namespace TextureEditor {

	public class paintingCollision {
		public StrokeVector vector;
		public PlaytimePainter painter;

		public paintingCollision (PlaytimePainter p){
			painter = p;
			vector = new StrokeVector ();
		}
	}

   // [ExecuteInEditMode]
    public class PainterBall : MonoBehaviour {

        public MeshRenderer rendy;

		public List<paintingCollision> paintingOn = new List<paintingCollision>();
        public BrushConfig brush = new BrushConfig();

        void TryAddPainterFrom( GameObject go) {
            PlaytimePainter target = go.GetComponent<PlaytimePainter>();

            if (target != null)   {
               
                paintingCollision col = new paintingCollision(target);
                paintingOn.Add(col);
                col.vector.posFrom = transform.position;
                col.vector.firstStroke = true;
                target.updateOrChangeDestination(texTarget.RenderTexture);
            }
        }

        public void OnCollisionEnter(Collision collision)
        {
            TryAddPainterFrom(collision.gameObject);
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
            // Hard code brush config here
            brush.brushType_rt = BrushTypeSphere.inst.index;
            if (rendy == null) 
                rendy = GetComponent<MeshRenderer>();
            rendy.material.color = brush.color.ToColor();

        }

        private void Update() {

            brush.Brush3D_Radius = transform.lossyScale.x;

			foreach (paintingCollision col in paintingOn){
				PlaytimePainter p = col.painter;
				if (p.isPaintingInWorldSpace(brush)) {
                   
                    StrokeVector v = col.vector;

                    v.posTo = transform.position;
					p.Paint (v, brush);
                    v.posFrom = v.posTo;
                    v.firstStroke = false;
				}
                else Debug.Log("not in world space");
            }
        }


        public void PEGI() {
            pegi.write("Painting on " + paintingOn.Count + " objects");

            pegi.newLine();
            pegi.write("Size:", 50);
            float size = transform.localScale.x;
            if (pegi.edit(ref size))
                transform.localScale = Vector3.one * size;
            pegi.newLine();

            brush.BrushIndependentTargets_PEGI();
            brush.Mode_Type_PEGI(brush.IndependentCPUblit);
            brush.currentBlitMode().PEGI(brush, null);
            if (brush.ColorSliders_PEGI()) 
                rendy.material.color = brush.color.ToColor();
            


        }

    }
}
