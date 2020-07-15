using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;

namespace PlaytimePainter.Examples { 

    [ExecuteInEditMode]
    public class PainterBall : MonoBehaviour  , IPEGI {
        
        public MeshRenderer rendy;
        public Rigidbody rigid;
        public SphereCollider _collider;

		public List<PaintingCollision> paintingOn = new List<PaintingCollision>();
        public Brush brush = new Brush();

        private void TryGetPainterFrom(GameObject go) {

            var target = go.GetComponent<PlaytimePainter>();

            if (!target || target.LockTextureEditing) return;

            var col = new PaintingCollision(target);
            paintingOn.Add(col);
            col.vector.posFrom = transform.position;
            col.vector.firstStroke = true;
            target.UpdateOrSetTexTarget(TexTarget.RenderTexture);
        }

        public void OnCollisionEnter(Collision collision) => TryGetPainterFrom(collision.gameObject);
        
        public void OnTriggerEnter(Collider collider) => TryGetPainterFrom(collider.gameObject);
         
        private void TryRemove(GameObject go)
        {
            foreach (var p in paintingOn)
                if (p.painter.gameObject == go)
                {
                    paintingOn.Remove(p);
                    return;
                }
        }

        public void OnTriggerExit(Collider exitedCollider) => TryRemove(exitedCollider.gameObject);
        
        public void OnCollisionExit(Collision exitedCollider) => TryRemove(exitedCollider.gameObject);
        
        public void OnEnable()  {
            brush.SetBrushType(false, BrushTypes.Sphere.Inst);

            if (!rendy) 
                rendy = GetComponent<MeshRenderer>();

            if (!rigid)
                rigid = GetComponent<Rigidbody>();

            if (!_collider)
                _collider = GetComponent<SphereCollider>();

            if (rendy)
                rendy.sharedMaterial.color = brush.Color;

            brush.targetIsTex2D = false;
        }

        private void Update() {

            brush.brush3DRadius = transform.lossyScale.x*1.4f;

			foreach (var col in paintingOn){
				var p = col.painter;

                if (!p.PaintCommand.SetBrush(brush).Is3DBrush) continue;

                var v = col.vector;
                v.posTo = transform.position;

                brush.Paint(p.PaintCommand.SetStroke(v));//v, p);

            }
        }

        public bool Inspect()
        {

            var changed = false;

            pegi.FullWindowService.DocumentationClickOpen(()=> "When colliding with other object will try to use sphere brush to paint on them." +
                                                       "Targets need to have PlaytimePainter component", "About Painter Ball");
     

            if (Application.isPlaying)
                "Painting on {0} objects".F(paintingOn.Count).nl();

            if (_collider.isTrigger && "Set as Rigid Collider object".Click().nl(ref changed))
            {
                _collider.isTrigger = false;
                rigid.isKinematic = false;
                rigid.useGravity = true;
            }

            if (!_collider.isTrigger && "Set as Trigger".Click().nl(ref changed))
            {
                _collider.isTrigger = true;
                rigid.isKinematic = true;
                rigid.useGravity = false;
            }

            var size = transform.localScale.x;

            if ("Size:".edit("Size of the ball", 50, ref size, 0.1f, 10).nl(ref changed))
                transform.localScale = Vector3.one * size;

            const string ballHint = "PaintBall_brushHint";

            "Painter ball made for World Space Brushes only".writeOneTimeHint(ballHint);

            if ((brush.Targets_PEGI().nl(ref changed)) || (brush.Mode_Type_PEGI().nl(ref changed)))
            {
                if (brush.targetIsTex2D || !brush.Is3DBrush())
                {
                    brush.targetIsTex2D = false;
                    brush.SetBrushType(false, BrushTypes.Sphere.Inst);

                    pegi.resetOneTimeHint(ballHint);
                }
            }

            if (brush.ColorSliders())
                rendy.sharedMaterial.color = brush.Color;

            return false;
        }

    }

    public class PaintingCollision
    {
        public Stroke vector;
        public PlaytimePainter painter;

        public PaintingCollision(PlaytimePainter p)
        {
            painter = p;
            vector = new Stroke();
        }
    }
}
