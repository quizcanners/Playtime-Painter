using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Painter{

[ExecuteInEditMode]
public class RenderBrush : MonoBehaviour {

		public static PainterManager rtp {get {return PainterManager.inst;}}

    public MeshRenderer meshRendy;
    public MeshFilter meshFilter;
    public Mesh modifiedMesh;
    public Bounds modifiedBound;
        PlaytimePainter replacedPainter;
        Material replacedMaterial;
        int replacedLayer;


    public void RestoreBounds() {

           if (replacedMaterial!= null) {
                replacedPainter.skinnedMeshRendy.sharedMaterial = replacedMaterial;
                replacedPainter.gameObject.layer = replacedLayer;
                replacedMaterial = null;
                replacedPainter.skinnedMeshRendy.localBounds = modifiedBound;
                meshRendy.enabled = true;
            } else
            {
                transform.parent = PainterManager.inst.transform;
                modifiedMesh.bounds = modifiedBound;
            }

        }



        public void CopyAllFrom (PlaytimePainter painter) {

        GameObject go = painter.gameObject;
            Transform camTransform = PainterManager.inst.transform;

            var skinny = painter.skinnedMeshRendy;
            if (skinny != null) {

                meshRendy.enabled = false;
                replacedPainter = painter;

                replacedMaterial = skinny.sharedMaterial;
                skinny.sharedMaterial = meshRendy.sharedMaterial;

                replacedLayer = go.layer;
                go.layer = gameObject.layer;
                
                modifiedBound = skinny.localBounds;
                skinny.localBounds  = new Bounds(painter.transform.InverseTransformPoint(camTransform.position + camTransform.forward * 100), Vector3.one * 500f);
               
            }
            else
            {
               
                Mesh mesh = painter.getMesh();

                Transform target = go.transform;

                transform.position = target.position;
                transform.rotation = target.rotation;
                transform.localScale = target.localScale;

                modifiedMesh = mesh;
                meshFilter.sharedMesh = mesh;
                
                modifiedBound = modifiedMesh.bounds;
                modifiedMesh.bounds = new Bounds(transform.InverseTransformPoint(camTransform.position + camTransform.forward * 100), Vector3.one * 500f);
                transform.parent = target.parent;
            }

       
    }

    public RenderBrush  Set(Shader shade) {
        meshRendy.sharedMaterial.shader = shade;
            return this;
    }

        public RenderBrush Set( Texture tex) {
            meshRendy.sharedMaterial.SetTexture("_MainTex", tex);
            return this;
        }


public void PrepareForFullCopyOf (Texture tex){
			float size = PainterManager.orthoSize * 2;
			float aspectRatio = (float)tex.width / (float)tex.height;
			transform.localScale = new Vector3(size * aspectRatio, size, 0);
			transform.localPosition = Vector3.forward * 10;
			transform.localRotation = Quaternion.identity;
            meshFilter.mesh = brushMeshGenerator.inst().GetQuad();
            Set(rtp.pixPerfectCopy).Set(tex);
        }




    private void OnEnable() {
     
    }

    // Use this for initialization
    void Awake () {
        if (meshRendy == null)
            meshRendy = GetComponent<MeshRenderer>();

        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
}